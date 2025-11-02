using System;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="DateTimeOffset"/> instances.
/// </summary>
public sealed class DateTimeOffsetFormatter : IFormatter<DateTimeOffset>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out DateTimeOffset value)
    {
        value = new DateTimeOffset(reader.ReadInt64(), TimeSpan.FromMinutes(reader.ReadInt16()));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in DateTimeOffset value)
    {
        writer.WriteInt64(value.Ticks);
        writer.WriteInt16((short)value.Offset.TotalMinutes);
    }
}