using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Helpers for common errors during serialization and deserialization.
/// </summary>
internal static class SerializationHelpers
{
    /// <summary>
    /// A generic method for getting the managed size of an object.
    /// </summary>
    private static readonly MethodInfo UnsafeSizeOf = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;

    /// <summary>
    /// Determines whether <paramref name="value"/> is an internal object that comes from the C# standard library.
    /// </summary>
    /// <param name="value">The value in question.</param>
    /// <returns><c>true</c> if <paramref name="value"/> is non-null, not publicly nameable, and a part of the <c>System</c> assembly.</returns>
    public static bool IsInternalSystemObject(object? value)
    {
        if (value is null)
        {
            return false;
        }
        else
        {
            var type = value.GetType();
            return !type.IsPublic && type.Assembly == typeof(object).Assembly;
        }
    }

    /// <summary>
    /// Gets the total number of bytes that one instance of <paramref name="type"/> takes in managed memory.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>The size of the type, in bytes.</returns>
    public static int SizeOf(Type type)
    {
        return (int)UnsafeSizeOf.MakeGenericMethod(type).Invoke(null, null)!;
    }
}