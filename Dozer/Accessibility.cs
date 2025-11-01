using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Determines which type members should be included or excluded
/// from serialization based upon their visibility.
/// </summary>
[Flags]
public enum Accessibility
{
    /// <summary>
    /// No members.
    /// </summary>
    None = 0,

    /// <summary>
    /// Members annotated with the <c>public</c> keyword.
    /// </summary>
    Public = 1 << 0,

    /// <summary>
    /// All non-public members (including <c>internal</c>, <c>protected</c>, and <c>private</c> members).
    /// </summary>
    NonPublic = 1 << 1,

    /// <summary>
    /// The default subset to include during serialization.
    /// </summary>
    Default = Public,

    /// <summary>
    /// All members.
    /// </summary>
    All = Public | NonPublic
}
