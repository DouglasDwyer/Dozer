using DouglasDwyer.Dozer.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Controls by-member serialization for types.
/// </summary>
internal sealed class ByMembersConfig
{
    /// <summary>
    /// Whether the output of <see cref="BlitFormatter{T}"/> and <see cref="ByMembersFormatter{T}"/>
    /// would be identical for this given type.
    /// </summary>
    public readonly bool Blittable;

    /// <summary>
    /// The fields and properties to include during serialization.
    /// </summary>
    public readonly IEnumerable<Member> IncludedMembers;

    /// <summary>
    /// The type that this configuration describes.
    /// </summary>
    public readonly Type Target;

    public ByMembersConfig(DozerSerializer serializer, Type type)
    {
        if (type.IsPrimitive)
        {
            throw new ArgumentException("Primitive types cannot be serialized by member", nameof(type));
        }

        var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => x is FieldInfo || x is PropertyInfo);
        IncludedMembers = members.Select(x => new Member(serializer, x)).ToArray();
        Target = type;
        Blittable = CanBlit(serializer, type, members);
    }

    /// <summary>
    /// Determines whether the output of <see cref="BlitFormatter{T}"/> and <see cref="ByMembersFormatter{T}"/>
    /// would be identical for the given type.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <param name="target">The type in question.</param>
    /// <param name="members">The members of the type included in serialization.</param>
    /// <returns>
    /// <c>true</c> if the type can be copied to/from memory verbatim.
    /// </returns>
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
                if (serializer.GetFormatter(field.FieldType) is not IBlitFormatter)
                {
                    return false;
                }

                nonPaddingBytes += SerializationHelpers.SizeOf(field.FieldType);
            }
        }

        return nonPaddingBytes == SerializationHelpers.SizeOf(target);
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
     todo
     
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
