using System;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Indicates that Dozer should serialize the marked field or property.
/// </summary>
/// <remarks>
/// This attribute can be added to private fields or properties.
/// Be aware that the data returned by the member (even if it's private) will be serialized and deserialized,
/// and thus can be viewed or intercepted by a malicious user or process.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DozerIncludeAttribute : Attribute { }