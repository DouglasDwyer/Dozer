namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes nullable types.
/// </summary>
/// <typeparam name="T">
/// The inner value type.
/// </typeparam>
public sealed class NullableFormatter<T> : IFormatter<T?> where T : struct
{
    /// <summary>
    /// The formatter to use for non-null values.
    /// </summary>
    private readonly IFormatter<T> _inner;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">
    /// The associated serializer.
    /// </param>
    public NullableFormatter(DozerSerializer serializer)
    {
        _inner = serializer.GetFormatter<T>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T? value)
    {
        var hasValue = reader.ReadBool();
        if (hasValue)
        {
            _inner.Deserialize(reader, out var result);
            value = result;
        }
        else
        {
            value = null;
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        writer.WriteBool(value.HasValue);
        if (value.HasValue)
        {
            _inner.Serialize(writer, value.Value);
        }
    }
}