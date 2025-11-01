using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Determines which fields to include during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DozerIncludeFieldsAttribute : Attribute
{
    /// <summary>
    /// Filters fields by accessibility.
    /// </summary>
    public readonly Accessibility Accessibility;

    /// <summary>
    /// Filters fields by mutability.
    /// </summary>
    public readonly FieldMutability Mutability;

    /// <inheritdoc cref="DozerIncludeFieldsAttribute(Accessibility, FieldMutability)"/>
    public DozerIncludeFieldsAttribute(Accessibility accessibility) : this(accessibility, FieldMutability.Default) { }

    /// <inheritdoc cref="DozerIncludeFieldsAttribute(Accessibility, FieldMutability)"/>
    public DozerIncludeFieldsAttribute(FieldMutability mutability) : this(Accessibility.Default, mutability) { }

    /// <summary>
    /// Determines which fields to include during serialization.
    /// </summary>
    /// <param name="accessibility">Filters by visibility.</param>
    /// <param name="mutability">Filters by mutability.</param>
    public DozerIncludeFieldsAttribute(Accessibility accessibility, FieldMutability mutability)
    {
        Accessibility = accessibility;
        Mutability = mutability;
    }
}
