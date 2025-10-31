using DouglasDwyer.Dozer.Formatters;
using System;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Create formatters derived from <see cref="MemberFormatter{T}"/>.
/// </summary>
public sealed class MemberResolver : IFormatterResolver
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
            var formatterType = typeof(MemberFormatter<>).MakeGenericType(type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
    }
}
