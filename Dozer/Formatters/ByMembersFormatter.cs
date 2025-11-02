using FastExpressionCompiler;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes a type by iterating over its individual members and serializing them.
/// </summary>
internal class ByMembersFormatter<T> : IFormatter<T>
{
    /// <summary>
    /// The <see cref="MethodInfo"/> for <see cref="RuntimeHelpers.GetUninitializedObject"/>.
    /// </summary>
    private static readonly MethodInfo GetUninitializedObject = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;

    /// <summary>
    /// The function type that is created for deserialization.
    /// </summary>
    /// <param name="reader">The binary input data.</param>
    /// <param name="value">The object that was deserialized.</param>
    private delegate void DeserializeDelegate(BufferReader reader, out T value);

    /// <summary>
    /// The function type that is created for serialization.
    /// </summary>
    /// <param name="writer">The binary output data.</param>
    /// <param name="value">The object to write.</param>
    private delegate void SerializeDelegate(BufferWriter writer, in T value);

    /// <summary>
    /// The compiled deserialization code for this type.
    /// </summary>
    private readonly DeserializeDelegate _deserialize;

    /// <summary>
    /// The compiled serialization code for this type.
    /// </summary>
    private readonly SerializeDelegate _serialize;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <exception cref="ArgumentException">If <typeparamref name="T"/> did not have a configuration from <paramref name="serializer"/>.</exception>
    public ByMembersFormatter(DozerSerializer serializer)
    {
        var config = serializer.GetTypeConfig(typeof(T));

        if (config is null)
        {
            throw new ArgumentException("Type did not have by-member config", nameof(T));
        }

        _deserialize = CompileDeserializer(config);
        _serialize = CompileSerializer(config);
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        _deserialize(reader, out value);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        _serialize(writer, value);
    }

    /// <summary>
    /// Compiles a deserializer for the provided type.
    /// </summary>
    /// <param name="config">The configuration of the type to serialize.</param>
    /// <returns>A delegate that can be called to deserialize the type.</returns>
    private static DeserializeDelegate CompileDeserializer(ByMembersConfig config)
    {
        var readerParam = Expression.Parameter(typeof(BufferReader), "reader");
        var valueParam = Expression.Parameter(config.Target.MakeByRefType(), "value");
        var newExpression = config.ConstructUninit
            ? (Expression)Expression.ConvertChecked(Expression.Call(GetUninitializedObject, Expression.Constant(config.Target)), config.Target)
            : Expression.New(config.Target);
        var body = Expression.Block(config.IncludedMembers.Select(x => CreateDeserializeExpression(x, readerParam, valueParam))
            .Prepend(Expression.Assign(valueParam, newExpression)));
        var lambda = Expression.Lambda<DeserializeDelegate>(body, readerParam, valueParam);
        return lambda.CompileFast(false, CompilerFlags.DisableInterpreter | CompilerFlags.ThrowOnNotSupportedExpression);
    }

    /// <summary>
    /// Compiles a serializer for the provided type.
    /// </summary>
    /// <param name="config">The configuration of the type to serialize.</param>
    /// <returns>A delegate that can be called to serialize the type.</returns>
    private static SerializeDelegate CompileSerializer(ByMembersConfig config)
    {
        var writerParam = Expression.Parameter(typeof(BufferWriter), "writer");
        var valueParam = Expression.Parameter(config.Target.MakeByRefType(), "value");
        var body = Expression.Block(config.IncludedMembers.Select(x => CreateSerializeExpression(x, writerParam, valueParam)));
        var lambda = Expression.Lambda<SerializeDelegate>(body, writerParam, valueParam);
        return lambda.CompileFast(false, CompilerFlags.DisableInterpreter | CompilerFlags.ThrowOnNotSupportedExpression);
    }

    /// <summary>
    /// Gets the concrete implementation of an interface method.
    /// </summary>
    /// <param name="implementingClass">The class implementing the interface.</param>
    /// <param name="interfaceMethod">The interface method definition.</param>
    /// <returns>The corresponding implementation method.</returns>
    /// <exception cref="ArgumentException">
    /// If <paramref name="interfaceMethod"/> was not an interface method.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// If <paramref name="implementingClass"/> did not implement the interface to which <paramref name="interfaceMethod"/> belongs.
    /// </exception>
    private static MethodInfo GetImplementationMethod(Type implementingClass, MethodInfo interfaceMethod)
    {
        if (interfaceMethod.DeclaringType is null || !interfaceMethod.DeclaringType.IsInterface)
        {
            throw new ArgumentException($"Expected method {interfaceMethod} to belong to interface");
        }

        if (!implementingClass.IsAssignableTo(interfaceMethod.DeclaringType))
        {
            throw new ArgumentException($"Expected implementing class {implementingClass} to be assignable to interface {interfaceMethod.DeclaringType}", nameof(implementingClass));
        }

        var map = implementingClass.GetInterfaceMap(interfaceMethod.DeclaringType);
        var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
        return map.TargetMethods[index];
    }

    /// <summary>
    /// Generates the deserialize expression for a member.
    /// </summary>
    /// <param name="entry">
    /// The member to deserialize.
    /// </param>
    /// <param name="reader">
    /// An expression representing the input <see cref="BufferReader"/>.
    /// </param>
    /// <param name="value">
    /// An expression representing the <c>out</c> value to deserialize.
    /// </param>
    /// <returns>
    /// The expression that was created.
    /// </returns>
    private static Expression CreateDeserializeExpression(ByMembersConfig.Member entry, Expression reader, Expression value)
    {
        switch (entry.Info)
        {
            case FieldInfo field:
                {
                    return Expression.Call(
                        Expression.Constant(entry.Formatter),
                        GetDeserializeImplementation(entry.Formatter, entry.Type),
                        reader,
                        Expression.Field(value, field));
                }
            case PropertyInfo property:
                var temp = Expression.Variable(entry.Type);
                var call = Expression.Call(
                    Expression.Constant(entry.Formatter),
                    GetDeserializeImplementation(entry.Formatter, entry.Type),
                    reader,
                    temp);
                var assign = Expression.Assign(Expression.Property(value, property), temp);
                return Expression.Block([temp], call, assign);
            default:
                throw new InvalidOperationException("Unrecognized member type");
        }
    }

    /// <summary>
    /// Generates the serialize expression for this member.
    /// </summary>
    /// <param name="entry">
    /// The member to serialize.
    /// </param>
    /// <param name="writer">
    /// An expression representing the output <see cref="BufferWriter"/>.
    /// </param>
    /// <param name="value">
    /// An expression representing the <c>out</c> value to deserialize.
    /// </param>
    /// <returns>
    /// The expression that was created.
    /// </returns>
    private static Expression CreateSerializeExpression(ByMembersConfig.Member entry, Expression writer, Expression value)
    {
        switch (entry.Info)
        {
            case FieldInfo field:
                return Expression.Call(
                    Expression.Constant(entry.Formatter),
                    GetSerializeImplementation(entry.Formatter, entry.Type),
                    writer,
                    Expression.Field(value, field));
            case PropertyInfo property:
                var temp = Expression.Variable(entry.Type);
                var assign = Expression.Assign(temp, Expression.Property(value, property));
                var call = Expression.Call(
                    Expression.Constant(entry.Formatter),
                    GetSerializeImplementation(entry.Formatter, entry.Type),
                    writer,
                    temp);
                return Expression.Block([temp], assign, call);
            default:
                throw new InvalidOperationException("Unrecognized member type");
        }
    }

    /// <summary>
    /// Gets the concrete deserialize method for a formatter.
    /// </summary>
    /// <param name="formatter">The formatter object.</param>
    /// <param name="targetType">The type to be deserialized.</param>
    /// <returns>
    /// The implementation of <see cref="IFormatter{T}.Deserialize(BufferReader, out T)"/> that will be invoked.
    /// </returns>
    private static MethodInfo GetDeserializeImplementation(object formatter, Type targetType)
    {
        return GetImplementationMethod(
            formatter.GetType(),
            typeof(IFormatter<>).MakeGenericType(targetType).GetMethod(nameof(IFormatter<bool>.Deserialize))!);
    }

    /// <summary>
    /// Gets the concrete serialize method for a formatter.
    /// </summary>
    /// <param name="formatter">The formatter object.</param>
    /// <param name="targetType">The type to be serialized.</param>
    /// <returns>
    /// The implementation of <see cref="IFormatter{T}.Serialize(BufferWriter, in T)"/> that will be invoked.
    /// </returns>
    private static MethodInfo GetSerializeImplementation(object formatter, Type targetType)
    {
        return GetImplementationMethod(
            formatter.GetType(),
            typeof(IFormatter<>).MakeGenericType(targetType).GetMethod(nameof(IFormatter<bool>.Serialize))!);
    }
}
