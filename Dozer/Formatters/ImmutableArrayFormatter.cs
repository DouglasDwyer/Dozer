using System.Collections.Immutable;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableArrayFormatter<T> : ImmutableCollectionFormatterBase<T, ImmutableArray<T?>, ImmutableArray<T?>.Builder>
{
    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableArrayFormatter(DozerSerializer serializer) : base(serializer) { }

    /// <inheritdoc/>
    protected override ImmutableArray<T?>.Builder DeserializeBuilder(BufferReader reader, int count) => ImmutableArray.CreateBuilder<T?>(count);

    /// <inheritdoc/>
    protected override ImmutableArray<T?> ToImmutable(ImmutableArray<T?>.Builder builder) => builder.ToImmutable();
}