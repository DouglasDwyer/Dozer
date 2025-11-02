using System;
using System.Numerics;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="BigInteger"/> instances.
/// </summary>
public sealed class BigIntegerFormatter : IFormatter<BigInteger>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out BigInteger value)
    {
        var length = (int)reader.ReadVarUInt32();
        value = new BigInteger(reader.Read(length));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in BigInteger value)
    {
        var span = writer.GetSpan(value.GetByteCount());
        if (!value.TryWriteBytes(span, out _))
        {
            throw new InvalidOperationException("Failed to encode big integer");
        }
        writer.Advance(span.Length);
    }
}
