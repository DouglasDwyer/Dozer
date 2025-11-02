using System;
using System.IO;
using System.Reflection;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="PropertyInfo"/> instances.
/// </summary>
internal sealed class PropertyInfoFormatter : IFormatter<PropertyInfo>
{
    /// <summary>
    /// Serializer for the declaring type.
    /// </summary>
    private readonly IFormatter<Type> _typeFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public PropertyInfoFormatter(DozerSerializer serializer)
    {
        _typeFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out PropertyInfo value)
    {
        _typeFormatter.Deserialize(reader, out var declaringType);
        var propertyName = reader.ReadString();
        var property = declaringType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        if (property is null)
        {
            throw new InvalidDataException($"Could not find property '{propertyName}' on {declaringType}");
        }

        value = property;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in PropertyInfo value)
    {
        _typeFormatter.Serialize(writer, value.DeclaringType!);
        writer.WriteString(value.Name);
    }
}
