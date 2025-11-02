using System;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes enums as their underlying integer values.
/// </summary>
/// <typeparam name="U">The integral representation of the enum.</typeparam>
/// <typeparam name="T">The enum to serialize.</typeparam>
internal sealed class EnumFormatter<U, T> : IFormatter<T> where U : struct where T : struct, Enum
{
    /// <summary>
    /// The formatter to use for the enum's inner representation.
    /// </summary>
    private readonly IFormatter<U> _integerFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="U"/> was not the integral representation of <typeparamref name="T"/>.
    /// </exception>
    public EnumFormatter(DozerSerializer serializer)
    {
        if (Enum.GetUnderlyingType(typeof(T)) != typeof(U))
        {
            throw new ArgumentException("Integral type did not match the inner representation of enum", nameof(U));
        }

        _integerFormatter = serializer.GetFormatter<U>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        _integerFormatter.Deserialize(reader, out var result);
        value = (T)(object)result;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        _integerFormatter.Serialize(writer, (U)(object)value);
    }
}
