[![Nuget](https://img.shields.io/nuget/v/DouglasDwyer.Dozer)](https://www.nuget.org/packages/DouglasDwyer.Dozer)
[![Downloads](https://img.shields.io/nuget/dt/DouglasDwyer.Dozer)](https://www.nuget.org/packages/DouglasDwyer.Dozer)

# 🚜 Dozer

#### Doug's binary serializer

Dozer is a general-purpose serializer for modern .NET, inspired by [Ceras](https://github.com/rikimaru0345/Ceras). It converts between C# objects and byte arrays, with support for:

1. **References and cycles:** all object references are preserved, including cyclic references. The entire object graph is represented in the serialized data.
1. **Polymorphic types:** base classes, boxed value types, and interfaces are all serializable. When necessary, Dozer includes `Type` and `Assembly` data in the binary output, so that types can be dynamically loaded during deserialization.
1. **Annotationless serialization:** by default, Dozer will serialize the public fields and auto properties on a type. No extra attributes are required to make a type serializable.
1. **Blitting `unmanaged` structs:** if type `T`'s binary format and managed representation are equivalent, then Dozer will copy the bytes of `T` and `T[]` verbatim. This eliminates the overhead of calling serialization methods for individual fields.

### Installation

This project is available on Nuget as `DouglasDwyer.Dozer`:

```bash
dotnet add package DouglasDwyer.Dozer
```

### How to use


Dozer is robust but quite simple to use. The following code snippet serializes and then deserializes an object, storing all of the object's data in a byte array and then retrieving it:

```csharp
var serializer = new DozerSerializer();
var objs = new List<object>() { 73, "hello", false };
var data = serializer.Serialize(objs);
var deserialized = serializer.Deserialize<List<object>>(data);
Console.WriteLine(objs.SequenceEqual(deserialized));  // true
```

For documentation about all functionality included in Dozer, please see the [complete API reference](https://douglasdwyer.github.io/Dozer/).

### Customizing serialization

#### Included fields and properties

By default, Dozer will serialize anytype with a public parameterless constructor. Dozer will include all public fields, auto get-set properties, and auto get-init properties.

```csharp
public class Foo
{
    // All of these will be serialized.
    public int Bar;
    public string Baz { get; set; }
    public double Buzz { get; init; }

    // None of these will be serialized.
    public readonly int Nope;
    public string Nah { get; }
    private float Huh;
}
```

The following attributes can be used to customize serialization, but are not required:

```csharp
[DozerConstructUninit]
public class Foo
{
    // This field will not be serialized
    [DozerExclude]
    public int Bar;
    
    // This field will be serialized
    [DozerInclude]
    private float Huh;

    // The class does not have a public constructor, and so would not be
    // serializable without the DozerConstructUninit attribute
    private Foo() { }
}

[DozerIncludeFields(Accessibility.All, FieldMutability.All)]
[DozerIncludeProperties(Accessibility.All, PropertyMutability.All)]
public struct Fizz
{
    // This will be serialized because of FieldMutability.All
    public readonly int Chef;

    // This will be serialized because of Accessibility.All
    private int Yert;
    
    // This will be serialized because of PropertyMutability.All
    public string Yah { get; }
}
```

#### Manually-defined formatter

Internally, serialization is driven by objects implementing the `IFormatter<T>` interface:

```csharp
public interface IFormatter<T> : IFormatter
{
    void Deserialize(BufferReader reader, out T value);

    void Serialize(BufferWriter writer, in T value);
}
```

The logic for serializing a type by its members (and interpreting the aforementioned attributes) [is implemented in a formatter](Dozer/Formatters/ByMembersFormatter.cs). To completely override the representation of a type, a custom formatter can be created. This formatter can be registered in two ways: using an [attribute](Dozer/Formatters/DefaultFormatterAttribute.cs), or by adding a resolver to the [`DozerSerializerOptions`](Dozer/DozerSerializerOptions.cs).

```csharp
// This attribute will replace the by-member formatter
[DefaultFormatter(MyClassFormatter)]
private class MyClass
{
    private object _someData;

    private sealed class MyClassFormatter : IFormatter<MyClass>
    {
        private readonly IFormatter<object> _someDataFormatter;

        // Formatters may be declared with either a parameterless constructor
        // or a constructor accepting the serializer
        public MyClassFormatter(DozerSerializer serializer)
        {
            // The serializer can be used to get formatters for children
            _someDataFormatter = serializer.GetFormatter<object>();
        }

        public void Deserialize(BufferReader reader, out MyClass value)
        {
            // Important! To support cyclic references, the output value
            // should be assigned before deserializing any child objects.
            value = new MyClass();

            _someDataFormatter.Deserialize(reader, value._someData);
            // Custom logic here
        }

        public void Serialize(BufferWriter writer, in MyClass value)
        {
            _someDataFormatter.Serialize(writer, value._someData);
            // Custom logic here
        }
    }
}
```

### Features

#### Comparison with other popular libraries

| Feature \ Library | [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp)  | [Ceras](https://github.com/rikimaru0345/Ceras) | Dozer |
| -------------  | ------------- | ------------- | ------------- |
| [References/cycles](## "Whether the library preserves the object graph") | ❌  | ✔️ | ✔️ |
| [Polymorphism](## "Whether the library can serialized derived classes and interface objects") | 🟡 (requires `Union` attribute or including the full type name for every serialized object) | ✔️ | ✔️ |
| [Annotationless serialization](## "Whether library-specific annotations must be added to serializable types") | ❌ | ✔️ | ✔️ |
| [Blitting `unmanaged` types](## "Whether unmanaged types can be copied from memory verbatim") | ❌ | 🟡 (unsafe handling of padding, `bool`, and `decimal`) | ✔️ |
| [Thread-safe](## "Whether a single serializer instance can be used across threads") | ✔️ | ❌ | ✔️ |
| [Standard types](## "Whether the library has out-of-the-box serializers for C# standard library types") | ✔️ | 🟡 (missing types from .NET 6+) | ✔️ |
| [Can serialize `System.Reflection` types](## "Whether the library can serialize assemblies, types, fields, etc.") | ❌ | ✔️ | ✔️ |
| [AoT support](## "Whether the library can work without dynamic code generation") | ✔️  | ✔️ | ❌ |
| [Version tolerance](## "Whether the fields of a single type can be changed without invalidating existing serialized data") | ✔️  | ✔️ | ❌ |
| [Last update](## "The last time that the repository had a commit") | ![Last Commit](https://img.shields.io/github/last-commit/MessagePack-CSharp/MessagePack-CSharp?color=lightgrey&label=Last%20commit&style=flat) | ![Last Commit](https://img.shields.io/github/last-commit/rikimaru0345/Ceras?color=lightgrey&label=Last%20commit&style=flat) | ![Last Commit](https://img.shields.io/github/last-commit/DouglasDwyer/Dozer?color=lightgrey&label=Last%20commit&style=flat) |

#### Improvements over MessagePack/Ceras

1. **Array covariance:** Dozer will properly preserve the types of covariant arrays: for example, a `string[]` serialized as type `object[]` will be deserialized with underlying type `string[]`. Using Ceras, the deserialized type would be `object[]`.
1. **Additional `System` types:** Dozer has builtin support for `ArraySegment<T>`, `CultureInfo`, `Memory<T>`, and `ReadOnlyMemory<T>`.
1. **Collection comparers:** Dozer will properly preserve the `IComparer`s and `IEqualityComparer`s for common collections (such as `Dictionary<K, V>`, `HashSet<T>` or `ImmutableSortedSet<T>`). Ceras does not include the comparer during serialization.
1. **Input interface:** for reading binary input, Dozer accepts a `ReadOnlySpan<byte>`. This allows for safely accepting input from unmanaged sources. In contrast, MessagePack requires a `ReadOnlyMemory<byte>` and Ceras requires a `byte[]`, both of which are more restrictive.
1. **Known types/assemblies:** to reduce binary size when encoding type information, Ceras allows the user to specify a `KnownTypes` list. Ceras associates the list order with integer IDs for each type - therefore, the list's order cannot be changed later. In contrast, Dozer accepts a set of `KnownAssemblies` from which it generates eight-byte type ID hashes. Adding known assemblies is much easier - it is not required to list every single type. This approach still works if assemblies are added, removed, or reordered, making it suitable even for dynamically-loaded assemblies (like plugins).
1. **Output interface:** for writing binary output, Dozer accepts an `IBufferWrite<byte>`. In contrast, Ceras requires a `byte[]`, which is more restrictive.
1. **Safe handling of blittable types:** Ceras will blit types containing padding (such as `struct Foo { byte A; int B; }`), which can expose the contents of uninitialized memory. Ceras will also blit types containing `bool` and `decimal` without validating that the bit patterns are valid (for instance, `bool` should only be `0` or `1`). Dozer checks for these cases - it will only blit structs when there is no padding and any bit pattern is valid.

### Thread-safety

All of Dozer's methods are completely thread-safe, and multiple objects may be serialized/deserialized using the same serializer instance at the same time. However, modifying an object that is currently being serialized may lead to unexpected results. While serialization will complete successfully, different parts of the object may be written to the output at different times. This means that a modification during serialization *could* lead to the serialization data coding for an object state that never actually existed in-memory. For example, consider the following:
- An `(int, string)` tuple of value `(0, "")` is passed into `DozerSerializer.Serialize()`. The initial serialization data contains the value `(?, ?)`, because neither field has been written yet.
- Another thread increments the `int` field, changing the state of the object to `(1, "")`.
- The serializer serializes the `int` field, and the serialization data is now `(1, ?)`.
- The other thread decrements the `int` field, and then sets the `string` to "hello", changing the state of the object to `(0, "hello")`.
- The serializer serializes the `string` field, resulting in a serialized object of `(1, "hello")`.

Though this situation is exceedingly rare, users should be wary of modifying their objects during serialization. Concurrent modifications can result in a serialized object whose state never existed in-memory; `(1, "hello")` was the result of the above example's serialization, but the object's value was never `(1, "hello")`.
