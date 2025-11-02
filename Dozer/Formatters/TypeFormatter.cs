using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes <see cref="Type"/> objects, including arrays and generics.
/// </summary>
internal sealed class TypeFormatter : IFormatter<Type>
{
    /// <summary>
    /// Formats assembly references.
    /// </summary>
    private readonly IFormatter<Assembly> _assemblyFormatter;

    /// <summary>
    /// Caches the arrays of generic type arguments returned by <see cref="Type.GetGenericArguments"/>.
    /// </summary>
    private readonly ConditionalWeakTable<Type, Type[]> _genericArgumentsCache;

    /// <summary>
    /// A lookup table between types and persistent hashes.
    /// Used to reduce the binary size for types from <see cref="DozerSerializerOptions.KnownAssemblies"/>.
    /// </summary>
    private readonly NameMap<Type> _knownTypes;

    /// <summary>
    /// Formats method references. Used when serializing the generic
    /// type parameters of methods.
    /// </summary>
    private readonly IFormatter<MethodBase> _methodFormatter;

    /// <summary>
    /// A reference serializer that will recursively fall back to this
    /// <see cref="TypeFormatter"/> when it encounters a new type.
    /// </summary>
    private readonly IFormatter<Type> _typeReferenceFormatter;

    /// <summary>
    /// Initializes a type formatter.
    /// </summary>
    /// <param name="serializer">
    /// The serializer associated with this formatter.
    /// </param>
    public TypeFormatter(DozerSerializer serializer)
    {
        _assemblyFormatter = serializer.GetFormatter<Assembly>();
        _genericArgumentsCache = new ConditionalWeakTable<Type, Type[]>();
        _knownTypes = new NameMap<Type>(
            serializer.Options.KnownAssemblies.Where(x => !x.IsDynamic).SelectMany(x => x.GetTypes()),
            PersistentTypeName);
        _methodFormatter = serializer.GetFormatter<MethodBase>();
        _typeReferenceFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Type value)
    {
        var metadata = (TypeMetadata)reader.ReadUInt8();
        switch (metadata.Kind)
        {
            case TypeKind.SZArray:
                {
                    _typeReferenceFormatter.Deserialize(reader, out var element);
                    value = element.MakeArrayType();
                    break;
                }
            case TypeKind.Array:
                {
                    _typeReferenceFormatter.Deserialize(reader, out var element);
                    value = element.MakeArrayType(metadata.Dimensions);
                    break;
                }
            case TypeKind.TypeParameter:
                {
                    _typeReferenceFormatter.Deserialize(reader, out var parent);
                    value = parent!.GetGenericArguments()[metadata.GenericIndex];
                    break;
                }
            case TypeKind.MethodParameter:
                {
                    _methodFormatter.Deserialize(reader, out var parent);
                    value = parent!.GetGenericArguments()[metadata.GenericIndex];
                    break;
                }
            case TypeKind.ConstructedGeneric:
                {
                    _typeReferenceFormatter.Deserialize(reader, out var definition);

                    var typeCount = _genericArgumentsCache.GetValue(definition!, x => x.GetGenericArguments()).Length;
                    var types = new Type[typeCount];

                    for (var i = 0; i < typeCount; i++)
                    {
                        _typeReferenceFormatter.Deserialize(reader, out var argument);
                        types[i] = argument;
                    }

                    value = definition.MakeGenericType(types);
                    break;
                }
            case TypeKind.BuiltinDefinition:
                {
                    var id = reader.ReadUInt16();
                    if (!BuiltinTypes.TryGetType(id, out value!))
                    {
                        throw new TypeLoadException("Could not find builtin type by ID");
                    }
                    break;
                }
            case TypeKind.KnownDefinition:
                {
                    var id = reader.ReadUInt64();
                    if (!_knownTypes.TryGetObject(id, out value!))
                    {
                        throw new TypeLoadException($"Could not find well-known type by hash; an assembly may be missing from the ${nameof(DozerSerializerOptions.KnownAssemblies)} list");
                    }
                    break;
                }
            case TypeKind.Definition:
            default:
                {
                    var fullName = reader.ReadString();
                    _assemblyFormatter.Deserialize(reader, out var assembly);
                    var result = assembly.GetType(fullName);

                    if (result is null)
                    {
                        throw new TypeLoadException($"Unable to load type {fullName} from {assembly.FullName}");
                    }

                    value = result;
                    break;
                }
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Type value)
    {
        if (value.IsSZArray)
        {
            writer.WriteUInt8((byte)TypeMetadata.SZArray());
            _typeReferenceFormatter.Serialize(writer, value.GetElementType()!);
        }
        else if (value.IsArray)
        {
            writer.WriteUInt8((byte)TypeMetadata.Array(value.GetArrayRank()));
            _typeReferenceFormatter.Serialize(writer, value.GetElementType()!);
        }
        else if (value.IsGenericTypeParameter)
        {
            writer.WriteUInt8((byte)TypeMetadata.TypeParameter(value.GenericParameterPosition));
            _typeReferenceFormatter.Serialize(writer, value.DeclaringType!);
        }
        else if (value.IsGenericMethodParameter)
        {
            writer.WriteUInt8((byte)TypeMetadata.MethodParameter(value.GenericParameterPosition));
            _methodFormatter.Serialize(writer, (MethodInfo)value.DeclaringMethod!);
        }
        else if (value.IsConstructedGenericType)
        {
            writer.WriteUInt8((byte)TypeMetadata.ConstructedGeneric());
            _typeReferenceFormatter.Serialize(writer, value.GetGenericTypeDefinition());

            foreach (var ty in _genericArgumentsCache.GetValue(value, x => x.GetGenericArguments()))
            {
                _typeReferenceFormatter.Serialize(writer, ty);
            }
        }
        else if (!value.ContainsGenericParameters || value.IsGenericTypeDefinition)
        {
            if (BuiltinTypes.TryGetId(value, out var builtinId))
            {
                writer.WriteUInt8((byte)TypeMetadata.BuiltinDefinition());
                writer.WriteUInt16(builtinId);
            }
            else if (_knownTypes.TryGetId(value, out var knownId))
            {
                writer.WriteUInt8((byte)TypeMetadata.KnownDefinition());
                writer.WriteUInt64(knownId);
            }
            else
            {
                var genericDefinition = value.IsGenericTypeDefinition ? value.GetGenericTypeDefinition() : value;
                writer.WriteUInt8((byte)TypeMetadata.Definition());
                writer.WriteString(genericDefinition.FullName!);
                _assemblyFormatter.Serialize(writer, genericDefinition.Assembly);
            }
        }
        else
        {
            throw new InvalidOperationException("Unrecognized kind of type");
        }
    }

    /// <summary>
    /// Gets an assembly-qualified name that can be used to identify a type
    /// across program versions.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>A stable name.</returns>
    private static string PersistentTypeName(Type type)
    {
        return $"[{type.Assembly.GetName().Name}]{type.FullName!}";
    }

    /// <summary>
    /// Identifies a specific subset of types.
    /// </summary>
    private enum TypeKind
    {
        /// <summary>
        /// An array type.
        /// </summary>
        Array,

        /// <summary>
        /// A constructed generic type.
        /// </summary>
        ConstructedGeneric,

        /// <summary>
        /// A non-generic type or an open generic type.
        /// </summary>
        Definition,

        /// <summary>
        /// A non-generic or open generic type from the <see cref="BuiltinTypes"/>.
        /// </summary>
        BuiltinDefinition,

        /// <summary>
        /// A non-generic or open generic type from one of the <see cref="DozerSerializerOptions.KnownAssemblies"/>.
        /// </summary>
        KnownDefinition,

        /// <summary>
        /// A generic method parameter.
        /// </summary>
        MethodParameter,

        /// <summary>
        /// A 1D array type with a lower bound of zero.
        /// </summary>
        SZArray,

        /// <summary>
        /// A generic type parameter.
        /// </summary>
        TypeParameter,
    }

    /// <summary>
    /// Records information about a type that is not already encoded in the name.
    /// This includes the number of generic parameters and array dimensions.
    /// </summary>
    private readonly record struct TypeMetadata
    {
        /// <summary>
        /// Describes what sort of type this is.
        /// </summary>
        public TypeKind Kind => (TypeKind)(_inner & 0b111);

        /// <summary>
        /// If <see cref="Kind"/> is <see cref="TypeKind.Array"/>, gets the total number of dimensions.
        /// </summary>
        public int Dimensions
        {
            get
            {
                if (Kind == TypeKind.Array)
                {
                    return (_inner >> 3) + 1;
                }
                else
                {
                    throw new InvalidOperationException("Type metadata was not of TypeKind.Array");
                }
            }
        }

        /// <summary>
        /// If <see cref="Kind"/> is <see cref="TypeKind.MethodParameter"/> or <see cref="TypeKind.TypeParameter"/>,
        /// gets the generic parameter index.
        /// </summary>
        public int GenericIndex
        {
            get
            {
                if (Kind == TypeKind.MethodParameter || Kind == TypeKind.TypeParameter)
                {
                    return _inner >> 3;
                }
                else
                {
                    throw new InvalidOperationException("Type metadata was not of TypeKind.MethodParameter or TypeKind.TypeParameter");
                }
            }
        }

        /// <summary>
        /// The inner representation of the metadata.
        /// </summary>
        private readonly byte _inner;

        /// <summary>
        /// Creates a new metadata object.
        /// </summary>
        /// <param name="inner">The inner representation of the metadata.</param>
        private TypeMetadata(byte inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Creates a new metadata object.
        /// </summary>
        /// <param name="kind">The subset to which this type belongs.</param>
        /// <param name="number">A numeric value associated with the type, used to encode extra properties.</param>
        private TypeMetadata(TypeKind kind, int number)
        {
            _inner = (byte)((byte)kind | (number << 3));
        }

        /// <summary>
        /// An array type.
        /// </summary>
        /// <param name="dimensions">The number of array dimensions.</param>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata Array(int dimensions)
        {
            return new TypeMetadata(TypeKind.Array, dimensions - 1);
        }

        /// <summary>
        /// A constructed generic type.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata ConstructedGeneric()
        {
            return new TypeMetadata(TypeKind.ConstructedGeneric, 0);
        }

        /// <summary>
        /// A non-generic type or an open generic type.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata Definition()
        {
            return new TypeMetadata(TypeKind.Definition, 0);
        }

        /// <summary>
        /// A non-generic or open generic type from the <see cref="BuiltinTypes"/>.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata BuiltinDefinition()
        {
            return new TypeMetadata(TypeKind.BuiltinDefinition, 0);
        }

        /// <summary>
        /// A non-generic or open generic type from one of the <see cref="DozerSerializerOptions.KnownAssemblies"/>.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata KnownDefinition()
        {
            return new TypeMetadata(TypeKind.KnownDefinition, 0);
        }

        /// <summary>
        /// A generic method parameter.
        /// </summary>
        /// <param name="index">The index of the parameter.</param>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata MethodParameter(int index)
        {
            return new TypeMetadata(TypeKind.MethodParameter, index);
        }

        /// <summary>
        /// An array type with variable lower bounds.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata SZArray()
        {
            return new TypeMetadata(TypeKind.SZArray, 0);
        }

        /// <summary>
        /// A generic type parameter.
        /// </summary>
        /// <param name="index">The index of the parameter.</param>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata TypeParameter(int index)
        {
            return new TypeMetadata(TypeKind.TypeParameter, index);
        }

        /// <summary>
        /// Converts the metadata to its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator byte(TypeMetadata value) => value._inner;

        /// <summary>
        /// Gets metadata from its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator TypeMetadata(byte value) => new TypeMetadata(value);
    }
}
