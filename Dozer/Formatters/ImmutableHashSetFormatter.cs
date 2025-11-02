using System.Collections.Generic;
using System.Collections.Immutable;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableHashSetFormatter<T> : ImmutableCollectionFormatterBase<T?, ImmutableHashSet<T?>, ImmutableHashSet<T?>.Builder>
{
    /// <summary>
    /// Gets the formatter to use for serializing key comparers.
    /// </summary>
    private readonly IFormatter<IEqualityComparer<T?>?> _comparerFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableHashSetFormatter(DozerSerializer serializer) : base(serializer)
    {
        _comparerFormatter = serializer.GetFormatter<IEqualityComparer<T?>?>();
    }

    /// <inheritdoc/>
    protected override ImmutableHashSet<T?>.Builder DeserializeBuilder(BufferReader reader, int count)
    {
        _comparerFormatter.Deserialize(reader, out var comparer);
        return ImmutableHashSet.CreateBuilder(comparer);
    }

    /// <inheritdoc/>
    protected override void SerializeBuilder(BufferWriter writer, in ImmutableHashSet<T?> value)
    {
        _comparerFormatter.Serialize(writer, SerializationHelpers.IsInternalSystemObject(value.KeyComparer) ? null : value.KeyComparer);
    }

    /// <inheritdoc/>
    protected override ImmutableHashSet<T?> ToImmutable(ImmutableHashSet<T?>.Builder builder) => builder.ToImmutable();
}