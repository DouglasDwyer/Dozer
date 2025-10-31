using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Indicates that Dozer should not serialize the marked field or property.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DozerExcludeAttribute : Attribute { }