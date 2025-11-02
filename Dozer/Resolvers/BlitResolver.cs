using DouglasDwyer.Dozer.Formatters;
using System;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Create formatters derived from <see cref="BlitFormatter{T}"/>.
/// </summary>
internal sealed class BlitResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(DozerSerializer serializer, Type type)
    {
        if (serializer.IsBlittable(type))
        {
            var formatterType = typeof(BlitFormatter<>).MakeGenericType(type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
        else
        {
            return null;
        }
    }
}
