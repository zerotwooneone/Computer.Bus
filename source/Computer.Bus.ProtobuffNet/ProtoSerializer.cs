using Computer.Bus.Contracts;
using ProtoBuf;

namespace Computer.Bus.ProtobuffNet;

public class ProtoSerializer : ISerializer
{
    public Task<byte[]> Serialize(string eventId, string correlationId)
    {
        return Serialize<string>(null, eventId, correlationId);
    }

    public Task<byte[]> Serialize<T>(T? param, string eventId, string correlationId)
    {
        try
        {
            using var memStream = new MemoryStream();
            var @event = new PublishEvent<T>(param, eventId, correlationId);
            Serializer.Serialize(memStream, @event);
            return Task.FromResult(memStream.ToArray());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<IBusEvent<T>?> Deserialize<T>(byte[] bytes)
    {
        using var memStream = new MemoryStream(bytes);

        var result = Serializer.Deserialize<ReceiveEvent<T>>(memStream);
        return Task.FromResult((IBusEvent<T>?)result);
    }
}