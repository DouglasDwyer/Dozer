using CommunityToolkit.HighPerformance.Buffers;
using DouglasDwyer.Dozer.Formatters;
using DouglasDwyer.Dozer.Resolvers;
using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Converts between objects and byte arrays. Holds all caches, formatters,
/// resolvers, and options necessary for serialization.
/// </summary>
public sealed class DozerSerializer
{
    /// <summary>
    /// Although these types are not sealed, their derived variants (like <c>RuntimeType</c>)
    /// are almost always internal to C#. As such, they are treated as sealed.
    /// </summary>
    internal static readonly ImmutableHashSet<Type> ArtificallySealedTypes = [
        typeof(Assembly),
        typeof(ConstructorInfo),
        typeof(FieldInfo),
        typeof(MethodBase),
        typeof(Module),
        typeof(Type),
    ];

    /// <summary>
    /// All primitive types that are blittable.
    /// Any <c>struct</c>s must be compositions of these types.
    /// </summary>
    private static readonly ImmutableHashSet<Type> BlittablePrimitiveTypes = BitConverter.IsLittleEndian ? [
        typeof(byte),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
        typeof(sbyte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(char),
    ] : [typeof(byte)];

    /// <summary>
    /// A list of builtin resolvers to use if no custom user-defined formatter is found.
    /// </summary>
    private ImmutableArray<IFormatterResolver> BuiltinResolvers = [
        new GenericResolver(typeof(AssemblyFormatter)),
        new GenericResolver(typeof(MethodBaseFormatter)),
        new GenericResolver(typeof(ModuleFormatter)),
        new GenericResolver(typeof(TypeFormatter)),
        new AttributeResolver(),
        new ArrayResolver(),
        new MemoryResolver(),
        new GenericResolver(typeof(ImmutableArrayFormatter<>)),
        new GenericResolver(typeof(ImmutableDictionaryFormatter<,>)),
        new GenericResolver(typeof(ImmutableHashSetFormatter<>)),
        new GenericResolver(typeof(ImmutableListFormatter<>)),
        new GenericResolver(typeof(ImmutableQueueFormatter<>)),
        new GenericResolver(typeof(ImmutableStackFormatter<>)),
        new GenericResolver(typeof(ImmutableSortedDictionaryFormatter<,>)),
        new GenericResolver(typeof(ImmutableSortedSetFormatter<>)),
        new GenericResolver(typeof(KeyValuePairFormatter<,>)),
        new GenericResolver(typeof(ListFormatter<>)),
        new GenericResolver(typeof(NullableFormatter<>)),
        new GenericResolver(typeof(QueueFormatter<>)),
        new GenericResolver(typeof(StackFormatter<>)),
        new SingletonResolver(new BigIntegerFormatter()),
        new SingletonResolver(new BitVector32Formatter()),
        new SingletonResolver(new CultureInfoFormatter()),
        new SingletonResolver(new DateTimeFormatter()),
        new SingletonResolver(new DateTimeOffsetFormatter()),
        new SingletonResolver(new GuidFormatter()),
        new SingletonResolver(new ReferenceEqualityComparerFormatter()),
        new SingletonResolver(new TimeSpanFormatter()),
        new ComparerCollectionResolver(),
        new CollectionResolver(),
        new BlitResolver(),
        new EnumResolver(),
        new SingletonResolver(new PrimitiveFormatter()),
        new ByMembersResolver(),
    ];

    /// <summary>
    /// The options that this serializer will use.
    /// </summary>
    public DozerSerializerOptions Options => new DozerSerializerOptions(_options);

    /// <summary>
    /// Formatters that are used to serialize the actual contents of objects.
    /// </summary>
    private readonly ConditionalWeakTable<Type, ContentFormatters> _contentFormatters;

    /// <summary>
    /// The options for this serializer.
    /// </summary>
    private readonly DozerSerializerOptions _options;

    /// <summary>
    /// Formatters that are used to serialize reference types and boxed value types.
    /// </summary>
    private readonly ConditionalWeakTable<Type, IFormatter> _referenceFormatters;

    /// <summary>
    /// Metadata that controls by-member serialization.
    /// </summary>
    private readonly ConditionalWeakTable<Type, ByMembersConfig?> _typeConfigs;

    /// <summary>
    /// Creates a new serializer with default options.
    /// </summary>
    public DozerSerializer() : this(new DozerSerializerOptions()) { }

    /// <summary>
    /// Creates a new serializer.
    /// </summary>
    /// <param name="options">
    /// Options that control serialization.
    /// </param>
    /// <exception cref="PlatformNotSupportedException">
    /// If the runtime does not support dynamic code generation.
    /// </exception>
    public DozerSerializer(DozerSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new PlatformNotSupportedException($"${nameof(DozerSerializer)} requires runtime support for dynamic code generation");
        }

        _contentFormatters = new ConditionalWeakTable<Type, ContentFormatters>();
        _options = options;
        _referenceFormatters = new ConditionalWeakTable<Type, IFormatter>();
        _typeConfigs = new ConditionalWeakTable<Type, ByMembersConfig?>();
    }

    /// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T)"/>
    public ArraySegment<byte> Serialize<T>(in T? value)
    {
        var writer = new ArrayBufferWriter<byte>();
        Serialize(writer, value);
        MemoryMarshal.TryGetArray(writer.WrittenMemory, out var result);
        return result;
    }

    /// <summary>
    /// Converts the provided value to binary data, writing the output
    /// to memory borrowed from <paramref name="pool"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="pool">The pool from which to borrow output buffer(s).</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// A memory owner referencing the borrowed byte array. This can be disposed
    /// to return the memory to the pool.
    /// </returns>
    public IMemoryOwner<byte> Serialize<T>(ArrayPool<byte> pool, in T? value)
    {
        var writer = new ArrayPoolBufferWriter<byte>(pool);

        try
        {
            Serialize(writer, value);
            return writer;
        }
        catch
        {
            writer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Converts the provided value to binary data.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="writer">Where the output data should be written.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// An array segment containing the serialized data.
    /// </returns>
    public void Serialize<T>(IBufferWriter<byte> writer, in T? value)
    {
        var context = SerializationContext.Pool.Get();
        try
        {
            var state = new BufferWriter.State(context, writer);
            GetFormatter<T?>().Serialize(new BufferWriter(ref state), value);
            state.Writer.Advance(state.CurrentBlockWritten);
        }
        finally
        {
            SerializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer must contain <b>exactly</b>
    /// the data for <typeparamref name="T"/>, and nothing else. To deserialize only a portion of a buffer,
    /// use the <see cref="Deserialize{T}(ref ReadOnlySpan{byte})"/> overload.
    /// </param>
    /// <returns>The generated object.</returns>
    /// <exception cref="InvalidDataException">
    /// If the input did not describe a valid object.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// If there was leftover data in the buffer after serialization.
    /// </exception>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to deserialize the type.
    /// </exception>
    public T Deserialize<T>(ReadOnlySpan<byte> data)
    {
        var result = Deserialize<T>(ref data);
        
        if (0 < data.Length)
        {
            throw new InvalidDataException("Deserialization did not consume all bytes in the provided data");
        }

        return result;
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer may be <b>larger</b>
    /// than the data for <typeparamref name="T"/>. It will be replaced with a span
    /// containing whatever data remained after deserialization.
    /// </param>
    /// <returns>The generated object.</returns>
    /// <exception cref="InvalidDataException">
    /// If the input did not describe a valid object.
    /// </exception>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize the type.
    /// </exception>
    public T Deserialize<T>(ref ReadOnlySpan<byte> data)
    {
        var context = DeserializationContext.Pool.Get();
        context.MaxAllocatedBytes = _options.MaxAllocatedBytes;

        try
        {
            var position = 0;
            GetFormatter<T>().Deserialize(new BufferReader(context, data, ref position), out var result);
            data = data[position..];
            return result!;
        }
        finally
        {
            DeserializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Gets the formatter to use when serializing objects with base type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The base class of all objects to be serialized.
    /// </param>
    /// <returns>
    /// The formatter to use. This can be cast to <see cref="IFormatter{T}"/> where <c>T</c> equals <paramref name="type"/>.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    public IFormatter GetFormatter(Type type)
    {
        if (type.IsValueType)
        {
            return _contentFormatters.GetValue(type, CreateContentFormatters).ContentFormatter;
        }
        else
        {
            return _referenceFormatters.GetValue(type, CreateReferenceFormatter);
        }
    }

    /// <summary>
    /// Gets the formatter to use when serializing objects with base type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The base class of all objects to be serialized.
    /// </typeparam>
    /// <returns>
    /// The formatter to use.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    public IFormatter<T> GetFormatter<T>()
    {
        return (IFormatter<T>)GetFormatter(typeof(T));
    }

    /// <summary>
    /// Gets the by-member serialization settings for this type.
    /// This includes which constructor to use and what members to include.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>
    /// The configuration to use, or <c>null</c> if <see cref="ByMembersFormatter{T}"/> is not applicable to this type.
    /// </returns>
    internal ByMembersConfig? GetTypeConfig(Type type)
    {
        return type.IsPrimitive ? null : _typeConfigs.GetValue(type, CreateTypeConfig);
    }

    /// <summary>
    /// Gets a type-erased dispatcher for serializing/deserializing
    /// objects whose true type is <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The actual, concrete object type.</param>
    /// <returns>
    /// A dispatcher that can be used to serialize and deserialize it.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    internal IFormatter<object> GetPolymorphicDispatcher(Type type)
    {
        return _contentFormatters.GetValue(type, CreateContentFormatters).PolymorphicDispatcher;
    }

    /// <summary>
    /// Determines whether the given type is blittable, without instantiating its formatter.
    /// This is based upon whether the type is a primitive, or whether its by-member configuration
    /// allows the entire <c>struct</c> to be serialized verbatim.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns><c>true</c> if a <see cref="BlitFormatter{T}"/> can be instantiated for this type.</returns>
    internal bool IsBlittable(Type type)
    {
        return BlittablePrimitiveTypes.Contains(type) || (GetTypeConfig(type)?.Blittable ?? false);
    }

    /// <summary>
    /// Generates new content formatters for the given type.
    /// </summary>
    /// <param name="type">
    /// The concrete type being serialized.
    /// </param>
    /// <returns></returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    private ContentFormatters CreateContentFormatters(Type type)
    {
        foreach (var sealedType in ArtificallySealedTypes)
        {
            if (type.IsAssignableTo(sealedType) && type != sealedType)
            {
                return CreateContentFormatters(sealedType);
            }
        }

        IFormatter? contentFormatter = null;
        foreach (var resolver in _options.Resolvers.Concat(BuiltinResolvers))
        {
            if (resolver.GetFormatter(this, type) is IFormatter newFormatter)
            {
                contentFormatter = newFormatter;
                break;
            }
        }

        if (contentFormatter is null)
        {
            throw new MissingFormatterException(type);
        }

        var polymorphicDispatcher = PolymorphicDispatcher.Create(type, contentFormatter);
        return new ContentFormatters { ContentFormatter = contentFormatter, PolymorphicDispatcher = polymorphicDispatcher };
    }

    /// <summary>
    /// Creates the reference formatter for the provided type.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>
    /// A formatter for serializing <paramref name="type"/> that respects object references.
    /// </returns>
    private IFormatter CreateReferenceFormatter(Type type)
    {
        return (IFormatter)Activator.CreateInstance(typeof(ReferenceFormatter<>).MakeGenericType(type), [this])!;
    }

    /// <summary>
    /// Generates the by-member configuration for a type.
    /// </summary>
    /// <param name="type">The type to serialize.</param>
    /// <returns>
    /// The configuration to use.
    /// </returns>
    private ByMembersConfig? CreateTypeConfig(Type type)
    {
        return ByMembersConfig.Load(this, type);
    }

    /// <summary>
    /// Holds formatters that are used to serialize the actual contents of an object.
    /// </summary>
    private class ContentFormatters
    {
        /// <summary>
        /// The "value" formatter that defines how to serialize the actual contents of this type.
        /// This formatter does not deal with references or polymorphism.
        /// </summary>
        public required IFormatter ContentFormatter { get; init; }

        /// <summary>
        /// A shim for invoking the <see cref="ContentFormatter"/> in a weakly-typed context.
        /// This is used when serializing polymorphic reference types and boxed value types.
        /// </summary>
        public required IFormatter<object> PolymorphicDispatcher { get; init; }
    }
}