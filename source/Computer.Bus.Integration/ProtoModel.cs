using System;
using ProtoBuf;

namespace Computer.Bus.Integration
{
    [ProtoContract]
    public class ProtoModel
    {
        [ProtoMember(1)] 
        public double fNumber { get; set; } = DateTime.Now.ToBinary();

        [ProtoMember(2)] 
        public string someString { get; set; } = "something";
        [ProtoMember(3)]
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}