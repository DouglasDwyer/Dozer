using System;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Formats span-like types, including <see cref="ArraySegment{T}"/>, <see cref="Memory{T}"/>, and <see cref="ReadOnlyMemory{T}"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="A">The span-like type.</typeparam>
public sealed class MemoryFormatter<T, A> : IFormatter<A>
{
    /// <summary>
    /// Formatter for serializing elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// A formatter that can serialize an entire fixed-size span with one call.
    /// This is primarily used as an optimization with <see cref="BlitFormatter{T}"/>.
    /// </summary>
    private readonly ISpanFormatter<T?>? _spanFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public MemoryFormatter(DozerSerializer serializer)
    {
        switch (default(A))
        {
            case ArraySegment<T>:
            case Memory<T>:
            case ReadOnlyMemory<T>:
                break;
            default:
                throw new ArgumentException($"Type {typeof(T)} was not the correct element type for {typeof(A)}", nameof(T));
        }

        _elementFormatter = serializer.GetFormatter<T?>();
        _spanFormatter = _elementFormatter as ISpanFormatter<T?>;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        var count = (int)reader.ReadVarUInt32();

        reader.Context.ConsumeBytes(count * Unsafe.SizeOf<T>());
        var backingArray = new T?[count];
        value = FromArray(backingArray);

        if (_spanFormatter is null)
        {
            for (var i = 0; i < backingArray.Length; i++)
            {
                _elementFormatter.Deserialize(reader, out backingArray[i]);
            }
        }
        else
        {
            _spanFormatter.Deserialize(reader, backingArray);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        var span = AsReadOnlySpan(value);
        writer.WriteVarUInt32((uint)span.Length);

        if (_spanFormatter is null)
        {
            for (var i = 0; i < span.Length; i++)
            {
                _elementFormatter.Serialize(writer, span[i]);
            }
        }
        else
        {
            _spanFormatter.Serialize(writer, span);
        }
    }

    /// <summary>
    /// Gets a span over the elements of <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source type.</param>
    /// <returns>A view over the data.</returns>
    private ReadOnlySpan<T?> AsReadOnlySpan(A source)
    {
        return source switch
        {
            ArraySegment<T?> seg => seg.AsSpan(),
            Memory<T?> mem => mem.Span,
            ReadOnlyMemory<T?> rom => rom.Span,
            _ => throw new InvalidOperationException($"Unsupported type {typeof(A)}")
        };
    }

    /// <summary>
    /// Creates an instance of <typeparamref name="A"/> using the given backing array.
    /// </summary>
    /// <param name="data">The array that holds the memory contents.</param>
    /// <returns>A view of type <typeparamref name="A"/> over <paramref name="data"/>.</returns>
    private A FromArray(T?[] data)
    {
        return default(A) switch
        {
            ArraySegment<T?> => (A)(object)(ArraySegment<T?>)data,
            Memory<T> => (A)(object)(Memory<T?>)data,
            ReadOnlyMemory<T> => (A)(object)(ReadOnlyMemory<T?>)data,
            _ => throw new InvalidOperationException($"Unsupported type {typeof(A)}")
        };
    }
}
