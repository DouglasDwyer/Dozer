using System.Collections.Specialized;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="BitVector32"/> instances.
/// </summary>
public sealed class BitVector32Formatter : IFormatter<BitVector32>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out BitVector32 value)
    {
        value = new BitVector32(reader.ReadInt32());
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in BitVector32 value)
    {
        writer.WriteInt32(value.Data);
    }
}