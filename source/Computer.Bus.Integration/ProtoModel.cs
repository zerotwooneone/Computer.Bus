using System;
using ProtoBuf;

namespace Computer.Bus.Integration;

[ProtoContract]
public record ProtoModel
{
    [ProtoMember(1)] public double fNumber { get; init; } = DateTime.Now.ToBinary();

    [ProtoMember(2)] public string someString { get; init; } = "something";
    [ProtoMember(3)] public DateTime Timestamp { get; init; } = DateTime.Now;
}