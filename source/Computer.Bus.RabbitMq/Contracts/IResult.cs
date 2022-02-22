namespace Computer.Bus.RabbitMq.Contracts;

public interface ISerializationResult<T>
{
    T? Param { get; }
    bool Success { get; }
    string? Reason { get; }
}

internal class SerializationResult<T> : ISerializationResult<T>
{
    public T? Param { get; init; }
    public bool Success { get; init; }
    public string? Reason { get; init; }
    private SerializationResult(T? param, bool success, string? reason = null)
    {
        Param = param;
        this.Success = success;
        this.Reason = reason;
    }

    internal static SerializationResult<T> CreateSuccess(T param)
    {
        return new SerializationResult<T>(param,true);
    }

    internal static SerializationResult<T> CreateError(string reason)
    {
        return new SerializationResult<T>(default,false, reason);
    }
}