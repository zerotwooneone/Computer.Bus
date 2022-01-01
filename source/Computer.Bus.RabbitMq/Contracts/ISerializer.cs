using System;
using System.Threading.Tasks;

namespace Computer.Bus.RabbitMq.Contracts;

public interface ISerializer
{
    Task<byte[]?> Serialize(string eventId, string correlationId);
    Task<byte[]?> Serialize(object param, Type type, string eventId, string correlationId);
    
    Task<IBusEvent?> Deserialize(byte[] bytes, Type? type = null);
}

public static class SerializerExtensions
{
    public static Task<byte[]?> Serialize<T>(this ISerializer serializer,
        T param,
        string eventId, string correlationId)
    {
        if (param == null)
        {
            return serializer.Serialize(eventId, correlationId);
        }
        return serializer.Serialize(param, typeof(T), eventId, correlationId);
    }

    public static async Task<IBusEvent<T>?> Deserialize<T>(this ISerializer serializer, byte[] bytes)
    {
        var @event = await serializer.Deserialize(bytes, typeof(T));
        if (@event?.Payload == null)
        {
            return null;
        }
        return new InnerBusEvent<T>((T)@event.Payload, @event.EventId, @event.CorrelationId);
    }

    internal record InnerBusEvent<T>(T Payload, string EventId, string CorrelationId) : IBusEvent<T>;
}