using System;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="DateTime"/> instances.
/// </summary>
internal sealed class DateTimeFormatter : IFormatter<DateTime>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out DateTime value)
    {
        value = DateTime.FromBinary(reader.ReadInt64());
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in DateTime value)
    {
        writer.WriteInt64(value.ToBinary());
    }
}