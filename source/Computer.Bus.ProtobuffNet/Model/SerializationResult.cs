using Computer.Bus.RabbitMq.Contracts;

namespace Computer.Bus.ProtobuffNet.Model;

internal class SerializationResult<T> : ISerializationResult<T>
{
    public T? Param { get; }
    public bool Success { get; }
    public string? Reason { get; }
    private SerializationResult(T? param, bool success, string? reason = null)
    {
        Param = param;
        Success = success;
        Reason = reason;
    }

    public static SerializationResult<T> CreateSuccess(T param)
    {
        return new SerializationResult<T>(param, true);
    }
    
    public static SerializationResult<T> CreateError(string reason)
    {
        return new SerializationResult<T>(default, false, reason);
    }
}