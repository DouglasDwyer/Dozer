using DouglasDwyer.Dozer.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Provides formatters for types implementing <see cref="ICollection{T}"/>.
/// </summary>
internal sealed class CollectionResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(DozerSerializer serializer, Type type)
    {
        var parameterlessConstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, []);
        var collectionInterface = type.GetInterfaces()
            .Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))
            .SingleOrDefault();

        if (parameterlessConstructor is not null && collectionInterface is not null)
        {
            var elementType = collectionInterface.GetGenericArguments()[0];
            var formatterType = typeof(CollectionFormatter<,>).MakeGenericType(elementType, type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
        else
        {
            return null;
        }
    }
}
