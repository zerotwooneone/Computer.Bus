namespace Computer.Bus.Contracts.Models;

internal class TypedResponse<T> : IResponse<T>
{
    private TypedResponse(T? obj, string? eventId, string? correlationId, bool success, string? errorReason)
    {
        Obj = obj;
        EventId = eventId;
        CorrelationId = correlationId;
        Success = success;
        ErrorReason = errorReason;
    }

    public T? Obj { get; }
    public string? EventId { get; }
    public string? CorrelationId { get; }
    public bool Success { get;  }
    public string? ErrorReason { get;  }

    public static TypedResponse<T> CreateSuccess(
        T? obj,
        string eventId,
        string correlationId)
    {
        return new TypedResponse<T>(obj, eventId, correlationId, true, null);
    }

    public static TypedResponse<T> CreateError(string errorReason, string? eventId = null, string? correlationId = null)
    {
        return new TypedResponse<T>(default, eventId, correlationId, false, errorReason);
    }
}