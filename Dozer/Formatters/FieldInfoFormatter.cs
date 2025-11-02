using System;
using System.IO;
using System.Reflection;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="FieldInfo"/> instances.
/// </summary>
internal sealed class FieldInfoFormatter : IFormatter<FieldInfo>
{
    /// <summary>
    /// Serializer for the declaring type.
    /// </summary>
    private readonly IFormatter<Type> _typeFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public FieldInfoFormatter(DozerSerializer serializer)
    {
        _typeFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out FieldInfo value)
    {
        _typeFormatter.Deserialize(reader, out var declaringType);
        var fieldName = reader.ReadString();
        var field = declaringType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        if (field is null)
        {
            throw new InvalidDataException($"Could not find field '{fieldName}' on {declaringType}");
        }

        value = field;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in FieldInfo value)
    {
        _typeFormatter.Serialize(writer, value.DeclaringType!);
        writer.WriteString(value.Name);
    }
}
