using System;
using System.Threading.Tasks;

namespace Computer.Bus.RabbitMq.Contracts;

public interface ISerializer
{
    Task<ISerializationResult<byte[]?>> Serialize(string eventId, string correlationId);
    Task<ISerializationResult<byte[]?>> Serialize(object param, Type type, string eventId, string correlationId);
    
    Task<ISerializationResult<IBusEvent>> Deserialize(byte[] bytes, Type? type = null);
}

public static class SerializerExtensions
{
    public static Task<ISerializationResult<byte[]?>> Serialize<T>(this ISerializer serializer,
        T param,
        string eventId, string correlationId)
    {
        if (param == null)
        {
            return serializer.Serialize(eventId, correlationId);
        }
        return serializer.Serialize(param, typeof(T), eventId, correlationId);
    }

    public static async Task<ISerializationResult<IBusEvent<T>>> Deserialize<T>(this ISerializer serializer, byte[] bytes)
    {
        var result = await serializer.Deserialize(bytes, typeof(T)).ConfigureAwait(false);
        if (!result.Success)
        {
            return SerializationResult<IBusEvent<T>>.CreateError(result.Reason ?? "serialization success was false, but reason was null");
        }
        if (result.Param?.Payload == null)
        {
            return SerializationResult<IBusEvent<T>>.CreateError("event was null");
        }
        return SerializationResult<IBusEvent<T>>.CreateSuccess(new InnerBusEvent<T>((T)result.Param.Payload, result.Param.EventId, result.Param.CorrelationId));
    }

    internal record InnerBusEvent<T>(T Payload, string EventId, string CorrelationId) : IBusEvent<T>;
}