using ProtoBuf;

namespace Computer.Bus.ProtobuffNet;

[ProtoContract]
public class PublishEvent<T>
{
    [ProtoMember(1)] public T? Value { get; }
    [ProtoMember(2)] public string EventId { get; }
    [ProtoMember(3)] public string CorrelationId { get; }

    public PublishEvent(T? value, string eventId, string correlationId)
    {
        Value = value;
        EventId = eventId;
        CorrelationId = correlationId;
    }
}