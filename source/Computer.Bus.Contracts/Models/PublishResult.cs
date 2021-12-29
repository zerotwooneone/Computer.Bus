namespace Computer.Bus.Contracts.Models;

public record PublishResult : IPublishResult
{
    public static readonly PublishResult SuccessResult = new(true);
    public bool Success { get; }
    public string? Reason { get; }

    private PublishResult(bool success, string? reason = null)
    {
        Success = success;
        Reason = reason;
    }

    public static PublishResult CreateError(string reason)
    {
        return new PublishResult(false, reason);
    }
}