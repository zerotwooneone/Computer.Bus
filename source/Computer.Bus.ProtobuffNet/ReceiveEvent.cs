using Computer.Bus.Contracts;
using ProtoBuf;

namespace Computer.Bus.ProtobuffNet;

[ProtoContract]
public class ReceiveEvent<T> : IBusEvent<T>
{
    [ProtoMember(1)] public T? Value { get; init; }
    [ProtoMember(2)] public string? EventId { get; init; }
    [ProtoMember(3)] public string? CorrelationId { get; init; }
}