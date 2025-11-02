using DouglasDwyer.Dozer.Formatters;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.Dozer.Resolvers;

/// <summary>
/// Generates formatters for span-like types.
/// </summary>
public sealed class MemoryResolver : IFormatterResolver
{
    /// <summary>
    /// The types supported by this resolver.
    /// </summary>
    private static readonly ImmutableHashSet<Type> SupportedTypes = [
        typeof(ArraySegment<>),
        typeof(Memory<>),
        typeof(ReadOnlyMemory<>),
    ];

    /// <inheritdoc/>
    public IFormatter? GetFormatter(DozerSerializer serializer, Type type)
    {
        if (type.IsConstructedGenericType && SupportedTypes.Contains(type.GetGenericTypeDefinition()))
        {
            var elementType = type.GetGenericArguments().Single();
            var formatterType = typeof(MemoryFormatter<,>).MakeGenericType(elementType, type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
        else
        {
            return null;
        }
    }
}
