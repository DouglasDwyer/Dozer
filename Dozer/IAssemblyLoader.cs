using System.Reflection;
using System.Runtime.Loader;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Dynamically resolves assemblies during unknown type deserialization.
/// The <see cref="ContextAssemblyLoader"/> is a default implementation of this interface,
/// and uses a <see cref="AssemblyLoadContext"/> to find new assemblies by name.
/// </summary>
public interface IAssemblyLoader
{
    /// <inheritdoc cref="AssemblyLoadContext.LoadFromAssemblyName"/>
    Assembly Load(AssemblyName assemblyName);
}