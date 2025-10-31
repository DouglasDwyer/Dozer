using DouglasDwyer.Dozer.Formatters;
using DouglasDwyer.Dozer.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.Dozer;

// todo: better name to emphasize that this is by-member configuration

/// <summary>
/// Controls by-member serialization for types.
/// </summary>
internal sealed class TypeConfig
{
    /// <summary>
    /// A generic method for getting the managed size of an object.
    /// </summary>
    private static readonly MethodInfo UnsafeSizeOf = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;

    public readonly bool Blittable;

    /// <summary>
    /// The fields and properties to include during serialization.
    /// </summary>
    public readonly IEnumerable<Member> IncludedMembers;

    /// <summary>
    /// The type that this configuration describes.
    /// </summary>
    public readonly Type Target;

    public TypeConfig(DozerSerializer serializer, Type type)
    {
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x is FieldInfo || x is PropertyInfo);
        IncludedMembers = members.Select(x => new Member(serializer, x)).ToArray();
        Target = type;
        Blittable = CanBlit(serializer, type, members);
    }

    // todo
    private static bool CanBlit(DozerSerializer serializer, Type target, IEnumerable<MemberInfo> members)
    {
        if (!target.IsValueType || !target.IsLayoutSequential)
        {
            return false;
        }

        var nonPaddingBytes = 0;
        foreach (var member in members)
        {
            if (member is FieldInfo field)
            {
                if (serializer.GetFormatter(field.FieldType) is not IBlitFormatter
                    && !DozerSerializer.BlittablePrimitiveTypes.Contains(target))
                {
                    return false;
                }

                nonPaddingBytes += ManagedSize(field.FieldType);
            }
        }

        return nonPaddingBytes == ManagedSize(target);
    }

    /// <summary>
    /// Gets a list of the types from which <paramref name="type"/> inherits,
    /// ordered from most-derived to least-derived.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>A list containing the type hirarchy, with <see cref="object"/> at the end.</returns>
    private static List<Type> GetInheritanceHierarchy(Type type)
    {
        var result = new List<Type>();
        var currentType = type;
        while (currentType != null)
        {
            result.Add(currentType);
            currentType = currentType.BaseType;
        }
        return result;
    }

    /// <summary>
    /// Gets the total number of bytes that one instance of <paramref name="type"/> takes in managed memory.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>The size of the type, in bytes.</returns>
    private static int ManagedSize(Type type)
    {
        return (int)UnsafeSizeOf.MakeGenericMethod(type).Invoke(null, null)!;
    }

    /// <summary>
    /// Describes a specific member to be serialized.
    /// </summary>
    public struct Member
    {
        /// <summary>
        /// The formatter object to use.
        /// </summary>
        public readonly IFormatter Formatter;

        /// <summary>
        /// The member to serialize.
        /// </summary>
        public readonly MemberInfo Info;

        /// <summary>
        /// The type of <see cref="Member"/>.
        /// </summary>
        public Type Type
        {
            get
            {
                switch (Info)
                {
                    case FieldInfo field:
                        return field.FieldType;
                    case PropertyInfo property:
                        return property.PropertyType;
                    default:
                        throw new InvalidOperationException("Unrecognized member type");
                }
            }
        }

        /// <summary>
        /// Creates a new member entry, loading the appropriate formatter for <paramref name="info"/>
        /// from <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The associated serializer.</param>
        /// <param name="info">The member to be serialized.</param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="info"/> was not a supported type.
        /// </exception>
        public Member(DozerSerializer serializer, MemberInfo info)
        {
            if (info is not FieldInfo && info is not PropertyInfo)
            {
                throw new ArgumentException($"Unsupported member type {info.GetType()}", nameof(info));
            }

            Info = info;
            Formatter = serializer.GetFormatter(Type);
        }
    }

    /*
     
     
    /// <summary>
    /// Creates <see cref="TypeConfig.Member"/> instances for all members to be serialized.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <param name="type">The type being formatted.</param>
    /// <param name="members">The members to include.</param>
    /// <returns>An array of all members, in a stable sorted order.</returns>
    private static TypeConfig.Member[] GetEntries(DozerSerializer serializer, Type type, IEnumerable<MemberInfo> members)
    {
        var inheritanceHierarchy = GetInheritanceHierarchy(type);
        return members.Distinct()
            .OrderBy(x => (inheritanceHierarchy.IndexOf(x.DeclaringType!), x.Name))
            .Select(x => new TypeConfig.Member(serializer, x))
            .ToArray();
    }

     */
}
