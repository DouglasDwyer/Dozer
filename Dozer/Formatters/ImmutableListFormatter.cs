using System.Collections.Immutable;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Handles serialization of an immutable collection.
/// </summary>
/// <typeparam name="T">
/// The element type being serialized.
/// </typeparam>
internal sealed class ImmutableListFormatter<T> : ImmutableCollectionFormatterBase<T, ImmutableList<T?>, ImmutableList<T?>.Builder>
{
    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ImmutableListFormatter(DozerSerializer serializer) : base(serializer) { }

    /// <inheritdoc/>
    protected override ImmutableList<T?>.Builder DeserializeBuilder(BufferReader reader, int count) => ImmutableList.CreateBuilder<T?>();

    /// <inheritdoc/>
    protected override ImmutableList<T?> ToImmutable(ImmutableList<T?>.Builder builder) => builder.ToImmutable();
}