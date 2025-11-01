using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.Dozer;

/// <summary>
/// Controls how <see cref="DozerSerializer"/> reads and writes objects.
/// </summary>
public class DozerSerializerOptions : ICloneable
{
    /// <summary>
    /// When the deserializer encounters unknown data, it may need to load types from new assemblies.
    /// This object can be used to control the loading process (and may be useful when deserializing data
    /// with dynamically-loaded assemblies or plugins).
    /// </summary>
    public IAssemblyLoader AssemblyLoader { get; set; }

    /// <summary>
    /// A predefined list of assemblies that are guaranteed to be present during serialization
    /// and deserialization. Type names from these assemblies will be encoded using a short, fixed-size hash.
    /// This can significantly reduce binary size, since otherwise the full type name must be recorded
    /// when serializing a polymorphic object.
    /// </summary>
    public IList<Assembly> KnownAssemblies { get; }

    /// <summary>
    /// User-defined resolvers that customize the serialization process for certain types.
    /// </summary>
    public IList<IFormatterResolver> Resolvers { get; }

    /// <summary>
    /// Generates the default serializer options. These options include:
    /// 
    /// <list type="bullet">
    /// <item>Using a <see cref="ContextAssemblyLoader"/></item>
    /// <item><c>System</c>, <c>System.Collections</c>, and <c>System.Collections.Generic</c> in <see cref="KnownAssemblies"/></item>
    /// <item>No custom resolvers</item>
    /// </list>
    /// </summary>
    public DozerSerializerOptions()
    {
        AssemblyLoader = new ContextAssemblyLoader();
        KnownAssemblies = [
            typeof(object).Assembly,
            typeof(IEnumerable).Assembly,
            typeof(IEnumerable<>).Assembly
        ];
        Resolvers = [];
    }

    /// <summary>
    /// Copies the options from a <see cref="DozerSerializerOptions"/> instance to a new instance.
    /// </summary>
    /// <param name="other">The source options.</param>
    public DozerSerializerOptions(DozerSerializerOptions other)
    {
        AssemblyLoader = other.AssemblyLoader;
        KnownAssemblies = other.KnownAssemblies.ToList();
        Resolvers = other.Resolvers.ToList();
    }

    /// <inheritdoc/>
    public object Clone() => new DozerSerializerOptions(this);
}
