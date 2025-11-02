using System;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="TimeSpan"/> instances.
/// </summary>
public sealed class TimeSpanFormatter : IFormatter<TimeSpan>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out TimeSpan value)
    {
        value = TimeSpan.FromTicks(reader.ReadInt64());
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in TimeSpan value)
    {
        writer.WriteInt64(value.Ticks);
    }
}