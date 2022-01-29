using System;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.Contracts;

public interface IRequestService
{
    Task<IResponse> Request(
        object? request, 
        Type requestType,
        string requestSubject,
        Type responseType,
        string responseSubject,
        string? eventId = null, 
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    
    Task<ISubscription> Listen(
        string requestSubject, 
        Type requestType,
        string responseSubject,
        Type responseType,
        CreateResponse createResponse);
    
    public delegate Task<(object?, Type)> CreateResponse(object? param, Type? type, string eventId, string correlationId);
}

public static class RequestServiceExtensions
{
    public static async Task<IResponse<TResponse>> Request<TRequest, TResponse>(this IRequestService requestService,
        TRequest? request,
        string requestSubject,
        string responseSubject,
        string? eventId = null, 
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var response = await requestService.Request(request,
            typeof(TRequest),
            requestSubject,
            typeof(TResponse),
            responseSubject,
            eventId,
            correlationId,
            cancellationToken).ConfigureAwait(false);
        if (response.EventId == null || response.CorrelationId == null)
        {
            return TypedResponse<TResponse>.CreateError(response.ErrorReason ?? "Response missing event or correlation id.",
                response.EventId,
                response.CorrelationId);
        }
        return TypedResponse<TResponse>.CreateSuccess((TResponse?)response.Obj, response.EventId, response.CorrelationId);
    }
    
    public delegate Task<TResponse?> CreateResponse<in TRequest, TResponse>(TRequest? param, string eventId, string correlationId);

    public static IDisposable Listen<TRequest, TResponse>(this IRequestService requestService,
        string requestSubject,
        string responseSubject,
        CreateResponse<TRequest, TResponse> createResponse)
    {
        async Task<(object?, Type)> InnerCallback(object? param, Type? type, string eventId, string correlationId)
        {
            var response = await createResponse((TRequest?)param, eventId, correlationId).ConfigureAwait(false);
            return (response, typeof(TResponse));
        }
        return requestService.Listen(requestSubject, typeof(TRequest), responseSubject, typeof(TResponse), InnerCallback);
    }
}