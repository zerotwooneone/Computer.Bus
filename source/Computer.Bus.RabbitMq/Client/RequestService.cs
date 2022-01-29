using System;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.RabbitMq.Client;

public class RequestService : IRequestService
{
    private readonly IBusClient _bus;

    public RequestService(IBusClient bus)
    {
        _bus = bus;
    }
    public async Task<IResponse> Request(
        object? request, 
        Type requestType, 
        string requestSubject, 
        Type responseType, 
        string responseSubject,
        string? eventId = null, 
        string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        var innerCorrelationId = correlationId ?? Guid.NewGuid().ToString();
        var innerResponseSubject = GetResponseSubject(responseSubject, innerCorrelationId);

        var responseTask = InnerSubscribe(responseType, innerResponseSubject, cancellationToken);
        
        var publishTask = InnerPublish(requestSubject, request, requestType, eventId, innerCorrelationId);

        await Task.WhenAll(responseTask, publishTask).ConfigureAwait(false);

        return responseTask.Result;
    }

    private async Task<IResponse> InnerSubscribe(
        Type responseType,
        string responseSubject,
        CancellationToken cancellationToken = default)
    {

        ISubscription? subscription = null;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.Token.Register(() => { subscription?.Dispose(); });
        var tcs = new TaskCompletionSource<IResponse>();

        GenericResponse? response = null;

        Task Callback(
            object? r,
            Type? rType,
            string eventId,
            string correlationId)
        {
            if (cts.Token.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cts.Token);
                return Task.CompletedTask;
            }

            cts.Cancel(); //cancel after first request
            response = GenericResponse.CreateSuccess(r, eventId, correlationId);
            tcs.TrySetResult(response);
            return Task.CompletedTask;
        }

        subscription = await _bus.Subscribe(responseSubject, responseType, Callback).ConfigureAwait(false);

        return await tcs.Task.ConfigureAwait(false);
    }

    private async Task<IPublishResult> InnerPublish(
        string subject, 
        object? obj, Type type,  
        string? eventId, string correlationId)
    {
        return await _bus.Publish(subject, obj, type, eventId, correlationId).ConfigureAwait(false);
    }

    public async Task<ISubscription> Listen(string requestSubject, Type requestType, string responseSubject, Type responseType,
        IRequestService.CreateResponse createResponse)
    {
        async Task Callback(object? request, Type? rType, string eventId, string correlationId)
        {
            var response = await createResponse(request, rType, eventId, correlationId).ConfigureAwait(false);
            if (!responseType.IsAssignableFrom(response.Item2))
            {
                throw new InvalidOperationException(
                    $"response type was unexpected. wanted:{responseType} got:{response.Item2}");
            }
            var innerResponseSubject = GetResponseSubject(responseSubject, correlationId);
            var s = await InnerPublish(innerResponseSubject,response.Item1, response.Item2, eventId: null, correlationId).ConfigureAwait(false);
            //todo: do something if publish failed
        }
        return await _bus.Subscribe(requestSubject, requestType, Callback).ConfigureAwait(false);
    }
    
    private static string GetResponseSubject(string responseSubject, string correlationId)
    {
        return $"{responseSubject}:{correlationId}";
    }
}