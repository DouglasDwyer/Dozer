using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Determines which fields should be included or excluded
/// from serialization based upon their mutability.
/// </summary>
[Flags]
public enum FieldMutability
{
    /// <summary>
    /// No fields.
    /// </summary>
    None = 0,

    /// <summary>
    /// The field is both readable and writable.
    /// </summary>
    Mutable = 1 << 0,

    /// <summary>
    /// The field is readable but not writable (i.e. marked <c>readonly</c>).
    /// </summary>
    InitOnly = 1 << 1,

    /// <summary>
    /// The default subset to include during serialization.
    /// </summary>
    Default = Mutable,

    /// <summary>
    /// All fields.
    /// </summary>
    All = Mutable | InitOnly
}
