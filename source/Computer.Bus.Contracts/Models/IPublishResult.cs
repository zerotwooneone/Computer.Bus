namespace Computer.Bus.Contracts.Models;

public interface IPublishResult
{
    public bool Success { get; }
    public string? Reason { get; }
}