using System.Collections.Generic;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of <see cref="KeyValuePair{TKey, TValue}"/> objects.
/// </summary>
/// <typeparam name="K">The key type.</typeparam>
/// <typeparam name="V">The value type.</typeparam>
internal sealed class KeyValuePairFormatter<K, V> : IFormatter<KeyValuePair<K?, V?>>
{
    /// <summary>
    /// Serializes keys.
    /// </summary>
    private readonly IFormatter<K?> _keyFormatter;

    /// <summary>
    /// Serializes values.
    /// </summary>
    private readonly IFormatter<V?> _valueFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public KeyValuePairFormatter(DozerSerializer serializer)
    {
        _keyFormatter = serializer.GetFormatter<K?>();
        _valueFormatter = serializer.GetFormatter<V?>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out KeyValuePair<K?, V?> value)
    {
        _keyFormatter.Deserialize(reader, out var pairKey);
        _valueFormatter.Deserialize(reader, out var pairValue);
        value = new KeyValuePair<K?, V?>(pairKey, pairValue);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in KeyValuePair<K?, V?> value)
    {
        _keyFormatter.Serialize(writer, value.Key);
        _valueFormatter.Serialize(writer, value.Value);
    }
}
