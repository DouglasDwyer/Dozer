[![Nuget](https://img.shields.io/nuget/v/DouglasDwyer.Dozer)](https://www.nuget.org/packages/DouglasDwyer.Dozer)
[![Downloads](https://img.shields.io/nuget/dt/DouglasDwyer.Dozer)](https://www.nuget.org/packages/DouglasDwyer.Dozer)

# Dozer

Dozer is a fast, efficient, customizable C# binary serializer that can serialize absolutely anything.

### Installation

Dozer can be obtained as a Nuget package. To import it into your project, either download DouglasDwyer.Dozer from the Visual Studio package manager or run the command `Install-Package DouglasDwyer.Dozer` using the package manager console.

### How to use

Dozer is robust but quite simple to use. The following code snippet serializes and then deserializes an object, storing all of the object's data in a byte array and then retrieving it:

```csharp
var serializer = new DozerSerializer();
var objs = new List<object>() { 73, "hello", false };
var data = serializer.Serialize(objs);
var deserialized = serializer.Deserialize<List<object>>(data);
Console.WriteLine(objs.SequenceEqual(deserialized));  //True
```

### Features

#### Functionality

Dozer is an object-oriented, reference-based binary serializer.

todo

#### Binary format

todo

#### Security

Serialization can be a dangerous affair, especially with a library as far-reaching/nonrestrictive as Dozer. As such, Dozer implements some key safety features to minimize the risk of serialization-based attacks. In addition to utilizing Dozer's safety features, all consumers of the library are encouraged to read more about serialization security.

todo

#### Thread-safety

All of Dozer's methods are completely thread-safe, and multiple objects may be serialized/deserialized using the same serializer instance at the same time. However, modifying an object that is currently being serialized may lead to unexpected results. While serialization will complete successfully, different parts of the object may be written to the byte array at different times. This means that a modification during serialization *could* lead to the serialization data coding for an object state that never actually existed in-memory. For example, consider the following:
- A new object, with an `int` field and a `string` field, is passed as an argument into a serializer's `Serialize()` method. The current state of the object will be denoted as (0, ""), where the first value is the `int` and the second the `string`. The initial serialization data contains the value (?, ?), because neither field has been written yet.
- Another thread increments the `int` field, changing the state of the object to (1, "").
- The serializer serializes the `int` field, and the serialization data is now (1, ?).
- The other thread decrements the `int` field, and then sets the `string` to "hello", changing the state of the object to (0, "hello").
- The serializer serializes the `string` field, resulting in a serialized object of (1, "hello").

Though this situation is exceedingly rare, users should be wary of modifying their objects during serialization. Concurrent modifications can result in a serialized object whose state never existed in-memory; (1, "hello") was the result of the above example's serialization, but the object's value was never (1, "hello").

### API Reference

For documentation about each type included in Dozer, please see the [complete API reference](https://douglasdwyer.github.io/Dozer/).

### Basic concepts

#### Serialization

todo

#### Deserialization

todo