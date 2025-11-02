using System;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Converts <see cref="Guid"/> instances.
/// </summary>
internal sealed class GuidFormatter : IFormatter<Guid>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Guid value)
    {
        value = new Guid(reader.Read(Unsafe.SizeOf<Guid>()));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Guid value)
    {
        var output = writer.GetSpan(Unsafe.SizeOf<Guid>());
        if (!value.TryWriteBytes(output))
        {
            throw new InvalidOperationException("Failed to write GUID bytes");
        }
        writer.Advance(output.Length);
    }
}
