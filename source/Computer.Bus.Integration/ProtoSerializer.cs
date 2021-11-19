using System;
using System.IO;
using Computer.Bus.RabbitMq.Serialize;
using ProtoBuf;

namespace Computer.Bus.Integration
{
    internal class ProtoSerializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            using var memStream = new MemoryStream();
            Serializer.Serialize<T>(memStream,obj);
            return memStream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using var memStream = new MemoryStream(bytes);
            var result = Serializer.Deserialize<T>(memStream);
            //Console.WriteLine($"byte count: {bytes.Length}");
            return result;
        }
    }
}