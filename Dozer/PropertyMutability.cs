using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Determines which properties should be included or excluded
/// from serialization based upon their mutability.
/// </summary>
[Flags]
public enum PropertyMutability
{
    /// <summary>
    /// No properties.
    /// </summary>
    None = 0,

    /// <summary>
    /// The property is an auto-implemented getter (i.e. <c>{ get; }</c>).
    /// </summary>
    Get = 1 << 0,

    /// <summary>
    /// The property is an auto-implemented getter/setter (i.e. <c>{ get; set; }</c>).
    /// </summary>
    GetSet = 1 << 1,

    /// <summary>
    /// The property is an auto-implemented getter/initializer (i.e. <c>{ get; init; }</c>).
    /// </summary>
    GetInit = 1 << 2,

    /// <summary>
    /// The property is an explicit getter/setter, without a compiler-generated backing field.
    /// </summary>
    GetSetExplicit = 1 << 3,

    /// <summary>
    /// All auto-implemented properties (anything that has one of <c>get; set; init;</c>).
    /// </summary>
    Auto = Get | GetSet | GetInit,

    /// <summary>
    /// The default subset to include during serialization.
    /// </summary>
    Default = GetSet | GetInit,

    /// <summary>
    /// All properties.
    /// </summary>
    All = Get | GetSet | GetInit | GetSetExplicit
}