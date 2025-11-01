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
    /// Gets the total number of bytes that one instance of <paramref name="type"/> takes in managed memory.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>The size of the type, in bytes.</returns>
    public static int SizeOf(Type type)
    {
        return (int)UnsafeSizeOf.MakeGenericMethod(type).Invoke(null, null)!;
    }

    /// <summary>
    /// If <paramref name="value"/> is null, then throws an exception.
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <param name="message">A message to include in the exception.</param>
    /// <exception cref="InvalidDataException">
    /// The exception that will be thrown.
    /// </exception>
    public static void ThrowIfNull([NotNull] object? value, string message)
    {
        if (value is null)
        {
            throw new InvalidDataException(message);
        }
    }
}