namespace Computer.Bus.Domain.Contracts;
using DtoPublishResult = Computer.Bus.Contracts.Models.IPublishResult;

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

    public static PublishResult FromDto(DtoPublishResult dto)
    {
        if (!dto.Success)
        {
            return CreateError(dto.Reason ?? "An unknown error occured translating Dto Result");
        }

        return SuccessResult;
    }
}