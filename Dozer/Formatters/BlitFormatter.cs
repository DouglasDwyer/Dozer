using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Copies the bytes of a type directly to/from memory.
/// </summary>
/// <typeparam name="T">
/// The type to be formatted. This must be a "blittable" type (it must be unmanaged,
/// and either a primitive or a struct with blittable fields).
/// </typeparam>
internal sealed class BlitFormatter<T> : IBlitFormatter, IFormatter<T>, ISpanFormatter<T> where T : unmanaged
{
    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="T"/> was not a blittable type.
    /// </exception>
    public BlitFormatter(DozerSerializer serializer)
    {
        if (!serializer.IsBlittable(typeof(T)))
        {
            throw new ArgumentException("Type was not blittable", nameof(T));
        }
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        value = MemoryMarshal.Read<T>(reader.Read(Unsafe.SizeOf<T>()));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, Span<T> elements)
    {
        var resultBytes = MemoryMarshal.AsBytes(elements);
        reader.Read(resultBytes.Length).CopyTo(resultBytes);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, ReadOnlySpan<T> elements)
    {
        writer.Write(MemoryMarshal.AsBytes(elements));
    }
}
