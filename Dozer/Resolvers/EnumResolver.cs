using DouglasDwyer.Dozer.Formatters;
using System;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Create formatters derived from <see cref="EnumFormatter{U, T}"/>.
/// </summary>
public sealed class EnumResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(DozerSerializer serializer, Type type)
    {
        if (type.IsEnum)
        {
            return (IFormatter?)Activator.CreateInstance(typeof(EnumFormatter<,>).MakeGenericType(Enum.GetUnderlyingType(type), type), serializer);
        }
        else
        {
            return null;
        }
    }
}
