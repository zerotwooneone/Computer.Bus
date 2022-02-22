using Computer.Bus.ProtobuffNet.Model;
using Computer.Bus.RabbitMq.Contracts;
using ProtoBuf;

namespace Computer.Bus.ProtobuffNet;

public class ProtoSerializer : ISerializer
{
    public Task<ISerializationResult<byte[]?>> Serialize(string eventId, string correlationId)
    {
        using var memStream = new MemoryStream();
        var @event = new PublishEvent{ Payload = null, EventId = eventId, CorrelationId = correlationId };
        Serializer.Serialize(memStream, @event);
            
        return Task.FromResult<ISerializationResult<byte[]?>>(SerializationResult<byte[]?>.CreateSuccess(memStream.ToArray()));
    }

    public Task<ISerializationResult<byte[]?>> Serialize(object? param, Type type, string eventId, string correlationId)
    {
        try
        {
            using var payloadStream = new MemoryStream();
            Serializer.Serialize(payloadStream, param);
            var payloadBytes = payloadStream.ToArray();

            using var memStream = new MemoryStream();
            var @event = new PublishEvent{ Payload = payloadBytes, EventId = eventId, CorrelationId = correlationId };
            Serializer.Serialize(memStream, @event);
            
            return Task.FromResult<ISerializationResult<byte[]?>>(SerializationResult<byte[]?>.CreateSuccess(memStream.ToArray()));
        }
        catch (Exception e)
        {
            return Task.FromResult<ISerializationResult<byte[]?>>(SerializationResult<byte[]?>.CreateError(e.ToString()));
        }
    }

    public async Task<ISerializationResult<IBusEvent>> Deserialize(byte[] bytes, Type? type = null)
    {
        await using var memStream = new MemoryStream(bytes);
        var @event = Serializer.Deserialize(typeof(ReceiveEvent),memStream) as ReceiveEvent;

        if (@event == null || 
            string.IsNullOrWhiteSpace(@event.EventId) ||
            string.IsNullOrWhiteSpace(@event.CorrelationId))
        {
            return SerializationResult<IBusEvent>.CreateError("eventId or correlationId was null");
        }

        if (@event.Payload == null ||
            type == null)
        {
            return SerializationResult<IBusEvent>.CreateSuccess(new InnerBusEvent(null, @event.EventId, @event.CorrelationId));
        }

        await using var payloadStream = new MemoryStream(@event.Payload);
        var result = Serializer.Deserialize(type, payloadStream);
        
        return SerializationResult<IBusEvent>.CreateSuccess(new InnerBusEvent(result, @event.EventId, @event.CorrelationId));
    }

    internal record InnerBusEvent(object? Payload, string EventId, string CorrelationId) : IBusEvent;
}