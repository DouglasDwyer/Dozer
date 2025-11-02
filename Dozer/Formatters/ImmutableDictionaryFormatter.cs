using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="K">
/// The key type being serialized.
/// </typeparam>
/// <typeparam name="V">
/// The value type being serialized.
/// </typeparam>
public sealed class ImmutableDictionaryFormatter<K, V> : ImmutableCollectionFormatterBase<KeyValuePair<K, V?>, ImmutableDictionary<K, V?>, ImmutableDictionary<K, V?>.Builder> where K : notnull
{
    /// <summary>
    /// Gets the formatter to use for serializing key comparers.
    /// </summary>
    private readonly IFormatter<IEqualityComparer<K>?> _keyComparerFormatter;

    /// <summary>
    /// Gets the formatter to use for serializing value comparers.
    /// </summary>
    private readonly IFormatter<IEqualityComparer<V?>?> _valueComparerFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableDictionaryFormatter(DozerSerializer serializer) : base(serializer)
    {
        _keyComparerFormatter = serializer.GetFormatter<IEqualityComparer<K>?>();
        _valueComparerFormatter = serializer.GetFormatter<IEqualityComparer<V?>?>();
    }

    /// <inheritdoc/>
    protected override ImmutableDictionary<K, V?>.Builder DeserializeBuilder(BufferReader reader, int count)
    {
        _keyComparerFormatter.Deserialize(reader, out var keyComparer);
        _valueComparerFormatter.Deserialize(reader, out var valueComparer);
        return ImmutableDictionary.CreateBuilder(keyComparer, valueComparer);
    }

    /// <inheritdoc/>
    protected override void SerializeBuilder(BufferWriter writer, in ImmutableDictionary<K, V?> value)
    {
        _keyComparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.KeyComparer) ? null : value.KeyComparer);
        _valueComparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.ValueComparer) ? null : value.ValueComparer);
    }

    /// <inheritdoc/>
    protected override ImmutableDictionary<K, V?> ToImmutable(ImmutableDictionary<K, V?>.Builder builder) => builder.ToImmutable();
}