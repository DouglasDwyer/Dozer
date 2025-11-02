using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableQueueFormatter<T> : IFormatter<ImmutableQueue<T?>>
{
    /// <summary>
    /// The inner formatter.
    /// </summary>
    private readonly IFormatter<T?[]> _arrayFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableQueueFormatter(DozerSerializer serializer)
    {
        _arrayFormatter = serializer.GetFormatter<T?[]>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ImmutableQueue<T?> value)
    {
        _arrayFormatter.Deserialize(reader, out var array);
        value = [.. array];
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ImmutableQueue<T?> value)
    {
        _arrayFormatter.Serialize(writer, value.ToArray());
    }
}
