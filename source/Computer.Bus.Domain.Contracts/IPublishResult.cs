namespace Computer.Bus.Domain.Contracts;

public interface IPublishResult
{
    public bool Success { get; }
    public string? Reason { get; }
}