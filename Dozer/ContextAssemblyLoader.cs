using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Fetches assemblies from an <see cref="AssemblyLoadContext"/>.
/// </summary>
public sealed class ContextAssemblyLoader : IAssemblyLoader
{
    /// <summary>
    /// Where to find new assemblies.
    /// </summary>
    private readonly AssemblyLoadContext _inner;

    /// <summary>
    /// Creates a new loader for the caller's <see cref="AssemblyLoadContext"/>.
    /// </summary>
    public ContextAssemblyLoader()
        : this(AssemblyLoadContext.GetLoadContext(GetOuterCallingAssembly()) ?? AssemblyLoadContext.Default)
    { }

    /// <summary>
    /// Creates a new loader.
    /// </summary>
    /// <param name="context">Where to find new assemblies.</param>
    public ContextAssemblyLoader(AssemblyLoadContext context)
    {
        _inner = context;
    }

    /// <inheritdoc/>
    public Assembly Load(AssemblyName assemblyName)
    {
        return _inner.LoadFromAssemblyName(assemblyName);
    }

    /// <summary>
    /// Attempts to find the most recent frame on the stack that is <b>not</b> from
    /// this assembly. Returns the assembly to which the frame belongs.
    /// </summary>
    /// <returns>
    /// The most recent assembly different from the current one.
    /// If no other assemblies are found, then returns the current assembly.
    /// </returns>
    private static Assembly GetOuterCallingAssembly()
    {
        var thisAssembly = typeof(ContextAssemblyLoader).Assembly;
        var trace = new StackTrace(1, false);
        var frames = trace.GetFrames();

        foreach (var frame in frames)
        {
            if (frame.GetMethod() is MethodBase method)
            {
                var assembly = method.Module.Assembly;
                if (assembly != thisAssembly)
                {
                    return assembly;
                }
            }
        }

        return thisAssembly;
    }
}
