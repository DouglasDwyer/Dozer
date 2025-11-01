using System;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Allows for serializing an object without a public default constructor.
/// The object will instead be creating by calling <see cref="RuntimeHelpers.GetUninitializedObject"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DozerConstructUninitAttribute : Attribute { }
