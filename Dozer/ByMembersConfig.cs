using DouglasDwyer.Dozer.Formatters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// Whether to call <see cref="RuntimeHelpers.GetUninitializedObject"/> to create the type
    /// rather than the default constructor.
    /// </summary>
    public readonly bool ConstructUninit;

    /// <summary>
    /// The fields and properties to include during serialization.
    /// </summary>
    public readonly ImmutableArray<Member> IncludedMembers;

    /// <summary>
    /// The type that this configuration describes.
    /// </summary>
    public readonly Type Target;

    /// <summary>
    /// Creates a new configuation.
    /// </summary>
    /// <param name="blittable">Whether the type is blittable.</param>
    /// <param name="constructUninit">Whether the type should be left uninitialized after allocation.</param>
    /// <param name="members">The members to serialize.</param>
    /// <param name="target">The target type itself.</param>
    private ByMembersConfig(bool blittable, bool constructUninit, ImmutableArray<Member> members, Type target)
    {
        Blittable = blittable;
        ConstructUninit = constructUninit;
        IncludedMembers = members;
        Target = target;
    }

    /// <summary>
    /// Attempts to load the by-members serialization config for a type.
    /// Returns <c>null</c> if the <see cref="ByMembersFormatter{T}"/> does not support the type.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <param name="type">The type to be serialized.</param>
    /// <returns>A configuration for the <see cref="ByMembersFormatter{T}"/>.</returns>
    public static ByMembersConfig? Load(DozerSerializer serializer, Type type)
    {
        if (type.IsPrimitive)
        {
            return null;
        }

        var constructUninit = type.GetCustomAttribute<DozerConstructUninitAttribute>() is not null;
        var canConstruct = constructUninit || type.IsValueType || type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, []) is not null;

        if (!canConstruct)
        {
            return null;
        }

        var members = GatherMembers(serializer, type);
        return new ByMembersConfig(
            CanBlit(serializer, type, members),
            constructUninit,
            members,
            type);
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
    private static bool CanBlit(DozerSerializer serializer, Type target, IEnumerable<Member> members)
    {
        if (!target.IsValueType || !target.IsLayoutSequential)
        {
            return false;
        }

        var nonPaddingBytes = 0;
        foreach (var member in members)
        {
            if (member.Info is FieldInfo field)
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
    /// Based upon type-level and member-level attributes, calculates the list of
    /// fields and properties to serialize.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <param name="type">The type being serialized.</param>
    /// <returns>
    /// An ordered list of the members to serialize.
    /// </returns>
    private static ImmutableArray<Member> GatherMembers(DozerSerializer serializer, Type type)
    {
        var inheritanceHierarchy = GetInheritanceHierarchy(type);
        return inheritanceHierarchy
            .SelectMany(baseTy => GatherFields(baseTy).Concat(GatherProperties(baseTy).Select(AutoPropertyToField)))
            .Distinct()
            .OrderBy(x => (inheritanceHierarchy.IndexOf(x.DeclaringType!), x.Name))
            .Select(x => new Member(serializer, x))
            .ToImmutableArray();
    }

    /// <summary>
    /// If <paramref name="info"/> is backed by a field, then returns the backing field.
    /// Otherwise, returns the property.
    /// </summary>
    /// <param name="info">
    /// The property to serialize.
    /// </param>
    /// <returns>
    /// Either the explicit <see cref="PropertyInfo"/> or the auto <see cref="FieldInfo"/>.
    /// </returns>
    private static MemberInfo AutoPropertyToField(PropertyInfo info)
    {
        if (GetBackingField(info) is FieldInfo field)
        {
            return field;
        }
        else
        {
            return info;
        }
    }

    /// <summary>
    /// Gathers all fields to include during serialization.
    /// </summary>
    /// <param name="type">The type to serialize.</param>
    /// <returns>
    /// An unordered collection of all fields to serialize.
    /// </returns>
    private static IEnumerable<FieldInfo> GatherFields(Type type)
    {
        var includeAttribute = type.GetCustomAttribute<DozerIncludeFieldsAttribute>();
        var allowedAccessibility = includeAttribute?.Accessibility ?? Accessibility.Default;
        var allowedMutability = includeAttribute?.Mutability ?? FieldMutability.Default;

        return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => !x.GetCustomAttributes<CompilerGeneratedAttribute>(true).Any())
            .Where(x =>
            {
                var forceExclude = x.GetCustomAttribute<DozerExcludeAttribute>() is not null;
                var forceInclude = x.GetCustomAttribute<DozerIncludeAttribute>() is not null;
                var include = allowedAccessibility.HasFlag(GetFieldAccessibility(x)) && allowedMutability.HasFlag(GetFieldMutability(x));
                return !forceExclude && (forceInclude || include);
            });
    }

    /// <summary>
    /// Determines the visibility of a field.
    /// </summary>
    /// <param name="info">The field in question.</param>
    /// <returns>
    /// How visible the member is.
    /// </returns>
    private static Accessibility GetFieldAccessibility(FieldInfo info)
    {
        return info.IsPublic ? Accessibility.Public : Accessibility.NonPublic;
    }

    /// <summary>
    /// Determines the mutability of a field.
    /// </summary>
    /// <param name="info">The field in question.</param>
    /// <returns>
    /// Whether the field can be read or written.
    /// </returns>
    private static FieldMutability GetFieldMutability(FieldInfo info)
    {
        return info.IsInitOnly ? FieldMutability.InitOnly : FieldMutability.Mutable;
    }

    /// <summary>
    /// Gathers all properties to include during serialization.
    /// </summary>
    /// <param name="type">The type to serialize.</param>
    /// <returns>
    /// An unordered collection of all properties to serialize.
    /// </returns>
    private static IEnumerable<PropertyInfo> GatherProperties(Type type)
    {
        var includeAttribute = type.GetCustomAttribute<DozerIncludePropertiesAttribute>();
        var allowedAccessibility = includeAttribute?.Accessibility ?? Accessibility.Default;
        var allowedMutability = includeAttribute?.Mutability ?? PropertyMutability.Default;

        return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => GetPropertyMutability(x) is not null)
            .Where(x =>
            {
                var forceExclude = x.GetCustomAttribute<DozerExcludeAttribute>() is not null;
                var forceInclude = x.GetCustomAttribute<DozerIncludeAttribute>() is not null;
                var include = allowedAccessibility.HasFlag(GetPropertyAccessibility(x)) && allowedMutability.HasFlag(GetPropertyMutability(x)!.Value);
                return !forceExclude && (forceInclude || include);
            });
    }

    /// <summary>
    /// Determines the visibility of a property.
    /// </summary>
    /// <param name="info">The property in question.</param>
    /// <returns>
    /// How visible the member is.
    /// </returns>
    private static Accessibility GetPropertyAccessibility(PropertyInfo info)
    {
        var getSetBothPublic = (info.GetMethod?.IsPublic ?? true)
            && (info.SetMethod?.IsPublic ?? true);
        return getSetBothPublic ? Accessibility.Public : Accessibility.NonPublic;
    }

    /// <summary>
    /// Determines the mutability of a property.
    /// </summary>
    /// <param name="info">The property in question.</param>
    /// <returns>
    /// When the field can be read or written, or <c>null</c> if no value of <see cref="PropertyMutability"/>
    /// could describe <paramref name="info"/> (this is the case for set-only properties).
    /// </returns>
    private static PropertyMutability? GetPropertyMutability(PropertyInfo info)
    {
        var isAuto = GetBackingField(info) is not null;

        return (isAuto, info.CanRead, info.CanWrite) switch
        {
            (_, false, _) => null,
            (true, true, true) => info.SetMethod!.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit))
                ? PropertyMutability.GetInit
                : PropertyMutability.GetSet,
            (true, true, false) => PropertyMutability.Get,
            (false, true, false) => null,
            (false, true, true) => PropertyMutability.GetSetExplicit
        };
    }

    /// <summary>
    /// Gets the backing field for <paramref name="info"/> if it is an auto property.
    /// </summary>
    /// <param name="info">The property in question.</param>
    /// <returns>
    /// The property's backing field, or <c>null</c> if <paramref name="info"/> was an explicit property.
    /// </returns>
    private static FieldInfo? GetBackingField(PropertyInfo info)
    {
        if (info.GetMethod?.GetCustomAttributes<CompilerGeneratedAttribute>(true).Any() ?? false)
        {
            var backingFieldName = $"<{info.Name}>k__BackingField";
            var field = info.DeclaringType!.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetCustomAttributes<CompilerGeneratedAttribute>(true).Any() ?? false)
            {
                return field;
            }
        }

        return null;
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
