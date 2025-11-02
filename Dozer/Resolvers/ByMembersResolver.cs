using DouglasDwyer.Dozer.Formatters;
using System;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Create formatters derived from <see cref="ByMembersFormatter{T}"/>.
/// </summary>
internal sealed class ByMembersResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(DozerSerializer serializer, Type type)
    {
        if (serializer.GetTypeConfig(type) is null)
        {
            return null;
        }
        else
        {
            var formatterType = typeof(ByMembersFormatter<>).MakeGenericType(type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
    }
}
