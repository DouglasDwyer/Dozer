using System.Collections.Generic;
using System.Collections.Immutable;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableSortedSetFormatter<T> : ImmutableCollectionFormatterBase<T?, ImmutableSortedSet<T?>, ImmutableSortedSet<T?>.Builder>
{
    /// <summary>
    /// Gets the formatter to use for serializing key comparers.
    /// </summary>
    private readonly IFormatter<IComparer<T?>?> _comparerFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableSortedSetFormatter(DozerSerializer serializer) : base(serializer)
    {
        _comparerFormatter = serializer.GetFormatter<IComparer<T?>?>();
    }

    /// <inheritdoc/>
    protected override ImmutableSortedSet<T?>.Builder DeserializeBuilder(BufferReader reader, int count)
    {
        _comparerFormatter.Deserialize(reader, out var comparer);
        return ImmutableSortedSet.CreateBuilder(comparer);
    }

    /// <inheritdoc/>
    protected override void SerializeBuilder(BufferWriter writer, in ImmutableSortedSet<T?> value)
    {
        _comparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.KeyComparer) ? null : value.KeyComparer);
    }

    /// <inheritdoc/>
    protected override ImmutableSortedSet<T?> ToImmutable(ImmutableSortedSet<T?>.Builder builder) => builder.ToImmutable();
}