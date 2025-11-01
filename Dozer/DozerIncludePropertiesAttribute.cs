using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Determines which properties to include during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DozerIncludePropertiesAttribute : Attribute
{
    /// <summary>
    /// Filters properties by accessibility.
    /// </summary>
    public readonly Accessibility Accessibility;

    /// <summary>
    /// Filters properties by mutability.
    /// </summary>
    public readonly PropertyMutability Mutability;

    /// <inheritdoc cref="DozerIncludeFieldsAttribute(Accessibility, FieldMutability)"/>
    public DozerIncludePropertiesAttribute(Accessibility accessibility) : this(accessibility, PropertyMutability.Default) { }

    /// <inheritdoc cref="DozerIncludeFieldsAttribute(Accessibility, FieldMutability)"/>
    public DozerIncludePropertiesAttribute(PropertyMutability mutability) : this(Accessibility.Default, mutability) { }

    /// <summary>
    /// Determines which fields to include during serialization.
    /// </summary>
    /// <param name="accessibility">Filters by visibility.</param>
    /// <param name="mutability">Filters by mutability.</param>
    public DozerIncludePropertiesAttribute(Accessibility accessibility, PropertyMutability mutability)
    {
        Accessibility = accessibility;
        Mutability = mutability;
    }
}
