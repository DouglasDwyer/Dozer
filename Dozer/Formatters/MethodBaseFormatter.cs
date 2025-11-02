using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes method references by their name and signature.
/// </summary>
internal sealed class MethodBaseFormatter : IFormatter<MethodBase>
{
    /// <summary>
    /// Flags for reflection that will capture all members.
    /// </summary>
    private const BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// A reference serializer that will recursively fall back to this
    /// <see cref="MethodBaseFormatter"/> when it encounters a new type.
    /// </summary>
    private readonly IFormatter<MethodBase> _methodReferenceFormatter;

    /// <summary>
    /// Formats module references. Used when serializing methods
    /// without a declaring type.
    /// </summary>
    private readonly IFormatter<Module> _moduleFormatter;

    /// <summary>
    /// Formats method references. Used when serializing the parameter types
    /// of methods.
    /// </summary>
    private readonly IFormatter<Type> _typeFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public MethodBaseFormatter(DozerSerializer serializer)
    {
        _methodReferenceFormatter = serializer.GetFormatter<MethodBase>();
        _moduleFormatter = serializer.GetFormatter<Module>();
        _typeFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out MethodBase value)
    {
        var kind = (MethodKind)reader.ReadUInt8();

        if (kind == MethodKind.ConstructedGeneric)
        {
            _methodReferenceFormatter.Deserialize(reader, out var definition);
            var genericArguments = new Type[definition!.GetGenericArguments().Length];

            for (var i = 0; i < genericArguments.Length; i++)
            {
                _typeFormatter.Deserialize(reader, out genericArguments[i]!);
            }

            value = ((MethodInfo)definition!).MakeGenericMethod(genericArguments);
        }
        else
        {
            object? declaringObject = null;
            var methods = Array.Empty<MethodBase>();

            if (kind == MethodKind.ModuleDefinition)
            {
                _moduleFormatter.Deserialize(reader, out var declaringModule);
                declaringObject = declaringModule;
                methods = declaringModule!.GetMethods(AllMembers);
            }
            else
            {
                _typeFormatter.Deserialize(reader, out var declaringType);
                declaringObject = declaringType;
                methods = kind == MethodKind.ConstructorDefinition ? declaringType.GetConstructors(AllMembers) : declaringType.GetMethods(AllMembers);
            }

            var name = reader.ReadString();
            var matcher = ReadGenericMatcher(reader);

            foreach (var method in methods)
            {
                if (method.Name == name && matcher(method))
                {
                    value = method;
                    return;
                }
            }

            throw new InvalidDataException($"Unable to load method {name} from {declaringObject}");
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in MethodBase value)
    {
        var genericArguments = value.IsGenericMethod ? value.GetGenericArguments() : Array.Empty<Type>();

        if (value.IsConstructedGenericMethod)
        {
            writer.WriteUInt8((byte)MethodKind.ConstructedGeneric);
            _methodReferenceFormatter.Serialize(writer, ((MethodInfo)value).GetGenericMethodDefinition());

            for (var i = 0; i < genericArguments.Length; i++)
            {
                _typeFormatter.Serialize(writer, genericArguments[i]);
            }
        }
        else
        {
            if (value.DeclaringType is null)
            {
                writer.WriteUInt8((byte)MethodKind.ModuleDefinition);
                _moduleFormatter.Serialize(writer, value.Module);
            }
            else
            {
                var kind = value.IsConstructor ? MethodKind.ConstructorDefinition : MethodKind.MemberDefinition;
                writer.WriteUInt8((byte)kind);
                _typeFormatter.Serialize(writer, value.DeclaringType);
            }

            writer.WriteString(value.Name);
            WriteGenericMatcher(writer, value);
        }
    }

    /// <summary>
    /// Reads a "matcher function" that can be used to determine whether a given
    /// method matches the serialized one. This is required to decode serialized generic method definitions,
    /// because generic methods have a cyclic dependency in the C# representation: generic methods
    /// depend upon their type parameters, but the type parameters depend on the generic methods.
    /// </summary>
    /// <param name="reader">The input data.</param>
    /// <returns>A function that will return <c>true</c> if method type is equivalent to the serialized one.</returns>
    private Func<MethodBase, bool> ReadGenericMatcher(BufferReader reader)
    {
        var genericParameterCount = reader.ReadUInt8();
        var parameterCount = reader.ReadUInt8();

        var matchers = new Func<Type, bool>[parameterCount];

        for (var i = 0; i < matchers.Length; i++)
        {
            matchers[i] = ReadGenericParameterMatcher(reader);
        }

        return method =>
        {
            var genericParameters = method.IsGenericMethod ? method.GetGenericArguments() : Array.Empty<Type>();
            var parameters = method.GetParameters();
            return genericParameters.Length == genericParameterCount
                && parameters.Length == parameterCount
                && matchers.Zip(parameters).All(x => x.First(x.Second.ParameterType));
        };
    }

    /// <summary>
    /// Reads the description of a "matcher function" that can be
    /// used to identify whether a parameter matches that of the serialized
    /// generic method definition.
    /// </summary>
    /// <param name="reader">The input buffer.</param>
    /// <returns>A function that will return <c>true</c> if the type is equivalent to the given parameter.</returns>
    private Func<Type, bool> ReadGenericParameterMatcher(BufferReader reader)
    {
        var metadata = (ParameterMetadata)reader.ReadUInt8();
        switch (metadata.Kind)
        {
            case ParameterKind.SZArray:
                {
                    var elementMatcher = ReadGenericParameterMatcher(reader);
                    return ty => ty.IsSZArray && elementMatcher(ty.GetElementType()!);
                }
            case ParameterKind.Array:
                {
                    var dimensions = metadata.Dimensions;
                    var elementMatcher = ReadGenericParameterMatcher(reader);
                    return ty => !ty.IsSZArray && ty.IsArray && ty.GetArrayRank() == dimensions && elementMatcher(ty.GetElementType()!);
                }
            case ParameterKind.TypeParameter:
                {
                    var index = metadata.GenericIndex;
                    var typeMatcher = ReadGenericParameterMatcher(reader);
                    return ty => ty.IsGenericTypeParameter && ty.GenericParameterPosition == index && typeMatcher(ty.DeclaringType!);
                }
            case ParameterKind.MethodParameter:
                {
                    var index = metadata.GenericIndex;
                    return ty => ty.IsGenericMethodParameter && ty.GenericParameterPosition == index;
                }
            case ParameterKind.ConstructedGeneric:
            default:
                {
                    _typeFormatter.Deserialize(reader, out var definition);
                    var parameterMatchers = new Func<Type, bool>[definition!.GetGenericArguments().Length];

                    for (var i = 0; i < parameterMatchers.Length; i++)
                    {
                        parameterMatchers[i] = ReadGenericParameterMatcher(reader);
                    }

                    return ty =>
                    {
                        var actualDefinition = ty.IsConstructedGenericType ? ty.GetGenericTypeDefinition() : ty;
                        return actualDefinition == definition
                            && parameterMatchers.Zip(ty.GetGenericArguments()).All(x => x.First(x.Second));
                    };
                }
        }
    }

    /// <summary>
    /// Writes a "matcher function" that can be used to determine whether a given
    /// method matches the serialized one. This is required to decode serialized generic method definitions,
    /// because generic methods have a cyclic dependency in the C# representation: generic methods
    /// depend upon their type parameters, but the type parameters depend on the generic methods.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="method">The method to be serialized.</param>
    private void WriteGenericMatcher(BufferWriter writer, MethodBase method)
    {
        if (method.IsConstructedGenericMethod)
        {
            throw new ArgumentException("Method must be generic definition", nameof(method));
        }

        var parameters = method.GetParameters();
        writer.WriteUInt8(method.IsGenericMethod ? (byte)method.GetGenericArguments().Length : (byte)0);
        writer.WriteUInt8((byte)parameters.Length);
        for (var i = 0; i < parameters.Length; i++)
        {
            WriteGenericParameterMatcher(writer, parameters[i].ParameterType);
        }
    }

    /// <summary>
    /// Writes the description of a "matcher function" that can be used
    /// to identify whether a parameter matches that of a generic method definition.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="parameterType">The parameter on the generic method to be matched.</param>
    private void WriteGenericParameterMatcher(BufferWriter writer, Type parameterType)
    {
        if (parameterType.IsSZArray)
        {
            writer.WriteUInt8((byte)ParameterMetadata.SZArray());
            WriteGenericParameterMatcher(writer, parameterType.GetElementType()!);
        }
        else if (parameterType.IsArray)
        {
            writer.WriteUInt8((byte)ParameterMetadata.Array(parameterType.GetArrayRank()));
            WriteGenericParameterMatcher(writer, parameterType.GetElementType()!);
        }
        else if (parameterType.IsGenericTypeParameter)
        {
            writer.WriteUInt8((byte)ParameterMetadata.TypeParameter(parameterType.GenericParameterPosition));
            WriteGenericParameterMatcher(writer, parameterType.DeclaringType!);
        }
        else if (parameterType.IsGenericMethodParameter)
        {
            writer.WriteUInt8((byte)ParameterMetadata.MethodParameter(parameterType.GenericParameterPosition));
        }
        else if (!parameterType.IsGenericType || parameterType.IsConstructedGenericType)
        {
            writer.WriteUInt8((byte)ParameterMetadata.ConstructedGeneric());
            _typeFormatter.Serialize(writer, parameterType.IsGenericType ? parameterType.GetGenericTypeDefinition() : parameterType);

            foreach (var ty in parameterType.GetGenericArguments())
            {
                WriteGenericParameterMatcher(writer, ty);
            }
        }
        else
        {
            throw new InvalidOperationException("Unrecognized kind of type");
        }
    }

    /// <summary>
    /// Identifies a specific subset of methods.
    /// </summary>
    private enum MethodKind
    {
        /// <summary>
        /// A type constructor.
        /// </summary>
        ConstructorDefinition,

        /// <summary>
        /// A constructed generic method.
        /// </summary>
        ConstructedGeneric,

        /// <summary>
        /// The method is a non-generic or open generic member of a type.
        /// </summary>
        MemberDefinition,

        /// <summary>
        /// The method is a non-generic or open generic member of a module,
        /// without a declaring type.
        /// </summary>
        ModuleDefinition
    }

    /// <summary>
    /// Identifies a specific subset of parameter types.
    /// </summary>
    private enum ParameterKind
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
    /// Records information about a parameter type that is not already encoded in the name.
    /// This includes the generic parameter index or array dimension count.
    /// </summary>
    private readonly record struct ParameterMetadata
    {
        /// <summary>
        /// Describes what sort of parameter type this is.
        /// </summary>
        public ParameterKind Kind => (ParameterKind)(_inner & 0b111);

        /// <summary>
        /// If <see cref="Kind"/> is <see cref="ParameterKind.Array"/>, gets the total number of dimensions.
        /// </summary>
        public int Dimensions
        {
            get
            {
                if (Kind == ParameterKind.Array)
                {
                    return (_inner >> 3) + 1;
                }
                else
                {
                    throw new InvalidOperationException("Parameter metadata was not of ParameterKind.Array");
                }
            }
        }

        /// <summary>
        /// If <see cref="Kind"/> is <see cref="ParameterKind.MethodParameter"/> or <see cref="ParameterKind.TypeParameter"/>,
        /// gets the generic parameter index.
        /// </summary>
        public int GenericIndex
        {
            get
            {
                if (Kind == ParameterKind.MethodParameter || Kind == ParameterKind.TypeParameter)
                {
                    return _inner >> 3;
                }
                else
                {
                    throw new InvalidOperationException("Parameter metadata not associated of ParameterKind.MethodParameter or ParameterKind.TypeParameter");
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
        private ParameterMetadata(byte inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Creates a new metadata object.
        /// </summary>
        /// <param name="kind">The subset to which this parameter type belongs.</param>
        /// <param name="number">A numeric value associated with the parameter type, used to encode extra properties.</param>
        private ParameterMetadata(ParameterKind kind, int number)
        {
            _inner = (byte)((byte)kind | (number << 3));
        }

        /// <summary>
        /// An array type.
        /// </summary>
        /// <param name="dimensions">The number of array dimensions.</param>
        /// <returns>The associated metadata.</returns>
        public static ParameterMetadata Array(int dimensions)
        {
            return new ParameterMetadata(ParameterKind.Array, dimensions - 1);
        }

        /// <summary>
        /// A constructed generic type.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static ParameterMetadata ConstructedGeneric()
        {
            return new ParameterMetadata(ParameterKind.ConstructedGeneric, 0);
        }

        /// <summary>
        /// A generic method parameter.
        /// </summary>
        /// <param name="index">The index of the parameter.</param>
        /// <returns>The associated metadata.</returns>
        public static ParameterMetadata MethodParameter(int index)
        {
            return new ParameterMetadata(ParameterKind.MethodParameter, index);
        }

        /// <summary>
        /// An array type with variable lower bounds.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static ParameterMetadata SZArray()
        {
            return new ParameterMetadata(ParameterKind.SZArray, 0);
        }

        /// <summary>
        /// A generic type parameter.
        /// </summary>
        /// <param name="index">The index of the parameter.</param>
        /// <returns>The associated metadata.</returns>
        public static ParameterMetadata TypeParameter(int index)
        {
            return new ParameterMetadata(ParameterKind.TypeParameter, index);
        }

        /// <summary>
        /// Converts the metadata to its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator byte(ParameterMetadata value) => value._inner;

        /// <summary>
        /// Gets metadata from its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator ParameterMetadata(byte value) => new ParameterMetadata(value);
    }
}