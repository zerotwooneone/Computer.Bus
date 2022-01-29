namespace Computer.Bus.Contracts.Models;

internal class GenericResponse : IResponse
{
    private GenericResponse(object? obj, string? eventId, string? correlationId, bool success, string? errorReason)
    {
        Obj = obj;
        EventId = eventId;
        CorrelationId = correlationId;
        Success = success;
        ErrorReason = errorReason;
    }

    public object? Obj { get; }
    public string? EventId { get; }
    public string? CorrelationId { get; }
    public bool Success { get;  }
    public string? ErrorReason { get;  }

    public static GenericResponse CreateSuccess(
        object? obj,
        string eventId,
        string correlationId)
    {
        return new GenericResponse(obj, eventId, correlationId, true, null);
    }

    public static GenericResponse CreateError(string errorReason, string? eventId = null, string? correlationId = null)
    {
        return new GenericResponse(default, eventId, correlationId, false, errorReason);
    }
}