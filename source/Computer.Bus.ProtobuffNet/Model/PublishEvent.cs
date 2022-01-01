using ProtoBuf;

namespace Computer.Bus.ProtobuffNet.Model;

[ProtoContract]
public class PublishEvent
{
    [ProtoMember(1)] public byte[]? Payload { get; init; }
    [ProtoMember(2)] public string EventId { get; init; } = Guid.NewGuid().ToString();
    [ProtoMember(3)] public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
}