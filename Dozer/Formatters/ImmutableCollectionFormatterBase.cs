using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Implements common functionality for immutable collection formatters.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="A">The immutable collection type itself.</typeparam>
/// <typeparam name="B">A mutable builder for the collection.</typeparam>
internal abstract class ImmutableCollectionFormatterBase<T, A, B> : IFormatter<A> where A : IReadOnlyCollection<T?> where B : ICollection<T?>
{
    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serializer"></param>
    internal ImmutableCollectionFormatterBase(DozerSerializer serializer)
    {
        _elementFormatter = serializer.GetFormatter<T?>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        var count = (int)reader.ReadVarUInt32();
        reader.Context.ConsumeBytes(count * Unsafe.SizeOf<T>());
        var builder = DeserializeBuilder(reader, count);

        for (var i = 0; i < count; i++)
        {
            _elementFormatter.Deserialize(reader, out var element);
            builder.Add(element);
        }

        value = ToImmutable(builder);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        writer.WriteVarUInt32((uint)value.Count);
        SerializeBuilder(writer, value);

        foreach (var element in value)
        {
            _elementFormatter.Serialize(writer, element);
        }
    }

    /// <summary>
    /// Creates a builder from serialized data.
    /// </summary>
    /// <param name="reader">The input buffer.</param>
    /// <param name="count">The number of elements that the collection will contain.</param>
    /// <returns>A mutable builder.</returns>
    protected abstract B DeserializeBuilder(BufferReader reader, int count);

    /// <summary>
    /// Writes any data to <paramref name="writer"/> necessary to create a new builder.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="value">The value for which a writer will be needed.</param>
    protected virtual void SerializeBuilder(BufferWriter writer, in A value) { }

    /// <summary>
    /// Finalizes the builder to get the immutable collection.
    /// </summary>
    /// <param name="builder">The builder with the elements.</param>
    /// <returns>The associated immutable data structure.</returns>
    protected abstract A ToImmutable(B builder);
}
