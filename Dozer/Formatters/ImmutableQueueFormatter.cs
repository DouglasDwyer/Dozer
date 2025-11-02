using System;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableStackFormatter<T> : IFormatter<ImmutableStack<T?>>
{
    /// <summary>
    /// The inner formatter.
    /// </summary>
    private readonly IFormatter<T?[]> _arrayFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableStackFormatter(DozerSerializer serializer)
    {
        _arrayFormatter = serializer.GetFormatter<T?[]>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ImmutableStack<T?> value)
    {
        _arrayFormatter.Deserialize(reader, out var array);
        value = [.. array.Reverse()];
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ImmutableStack<T?> value)
    {
        _arrayFormatter.Serialize(writer, value.ToArray());
    }
}
