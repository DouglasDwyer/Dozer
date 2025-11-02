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
internal sealed class ImmutableSortedDictionaryFormatter<K, V> : ImmutableCollectionFormatterBase<KeyValuePair<K, V?>, ImmutableSortedDictionary<K, V?>, ImmutableSortedDictionary<K, V?>.Builder> where K : notnull
{
    /// <summary>
    /// Gets the formatter to use for serializing key comparers.
    /// </summary>
    private readonly IFormatter<IComparer<K>?> _keyComparerFormatter;

    /// <summary>
    /// Gets the formatter to use for serializing value comparers.
    /// </summary>
    private readonly IFormatter<IEqualityComparer<V?>?> _valueComparerFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableSortedDictionaryFormatter(DozerSerializer serializer) : base(serializer)
    {
        _keyComparerFormatter = serializer.GetFormatter<IComparer<K>?>();
        _valueComparerFormatter = serializer.GetFormatter<IEqualityComparer<V?>?>();
    }

    /// <inheritdoc/>
    protected override ImmutableSortedDictionary<K, V?>.Builder DeserializeBuilder(BufferReader reader, int count)
    {
        _keyComparerFormatter.Deserialize(reader, out var keyComparer);
        _valueComparerFormatter.Deserialize(reader, out var valueComparer);
        return ImmutableSortedDictionary.CreateBuilder(keyComparer, valueComparer);
    }

    /// <inheritdoc/>
    protected override void SerializeBuilder(BufferWriter writer, in ImmutableSortedDictionary<K, V?> value)
    {
        _keyComparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.KeyComparer) ? null : value.KeyComparer);
        _valueComparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.ValueComparer) ? null : value.ValueComparer);
    }

    /// <inheritdoc/>
    protected override ImmutableSortedDictionary<K, V?> ToImmutable(ImmutableSortedDictionary<K, V?>.Builder builder) => builder.ToImmutable();
}