using System.IO;
using System.Reflection;

namespace DouglasDwyer.Dozer.Formatters;

/// <summary>
/// Serializes and deserializes <see cref="ParameterInfo"/> instances.
/// </summary>
internal sealed class ParameterInfoFormatter : IFormatter<ParameterInfo>
{
    /// <summary>
    /// Serializer for the declaring member.
    /// </summary>
    private readonly IFormatter<MemberInfo> _memberFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ParameterInfoFormatter(DozerSerializer serializer)
    {
        _memberFormatter = serializer.GetFormatter<MemberInfo>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ParameterInfo value)
    {
        _memberFormatter.Deserialize(reader, out var declaringMember);
        var position = reader.ReadUInt8();

        switch (declaringMember)
        {
            case MethodBase method:
                value = method.GetParameters()[position];
                break;
            case PropertyInfo property:
                value = property.GetIndexParameters()[position];
                break;
            default:
                throw new InvalidDataException($"Unrecognized declaring member type '{declaringMember.GetType()}' for {nameof(ParameterInfo)}");
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ParameterInfo value)
    {
        _memberFormatter.Serialize(writer, value.Member);
        writer.WriteUInt8((byte)value.Position);
    }
}
