using System;
using System.IO;
using System.Reflection;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="Module"/> objects.
/// </summary>
public sealed class ModuleFormatter : IFormatter<Module>
{
    /// <summary>
    /// Serializes assembly objects.
    /// </summary>
    private readonly IFormatter<Assembly?> _assemblyFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ModuleFormatter(DozerSerializer serializer)
    {
        _assemblyFormatter = serializer.GetFormatter<Assembly>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Module value)
    {
        _assemblyFormatter.Deserialize(reader, out var assembly);

        if (assembly is null)
        {
            throw new InvalidDataException("Module was not encoded properly: expected assembly, but got null");
        }

        var moduleName = reader.ReadString();
        var result = assembly.GetModule(moduleName);

        if (result is null)
        {
            throw new TypeLoadException($"Could not find module {moduleName} in {assembly}");
        }

        value = result;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Module value)
    {
        _assemblyFormatter.Serialize(writer, value.Assembly);
        writer.WriteString(value.ScopeName);
    }
}
