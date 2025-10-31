using DouglasDwyer.Dozer.Formatters;
using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Customizes how a type is formatted by the <see cref="MemberFormatter{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class DozerConfigAttribute : Attribute
{
}
