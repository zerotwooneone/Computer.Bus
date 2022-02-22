namespace Computer.Bus.Domain.Contracts;

public interface IBus
{
    Task<IPublishResult> Publish(string subjectId,
        string? eventId = null, string? correlationId = null);

    Task<IPublishResult> Publish(string subjectId,
        object param, Type type,
        string? eventId = null, string? correlationId = null);

    Task<ISubscription> Subscribe(string subjectId, Type type, 
        SubscribeCallbackP callback,
        ErrorCallback? errorCallback = null);
    Task<ISubscription> Subscribe(string subjectId, 
        SubscribeCallbackNp callback,
        ErrorCallback? errorCallback = null);
    
    public delegate Task SubscribeCallbackP(object? param, Type? type, string eventId, string correlationId);
    public delegate Task SubscribeCallbackNp(string eventId, string correlationId);
    public delegate void ErrorCallback(string reason, object? param, Type? type, string? eventId, string? correlationId);
}

public static class BusClientExtensions {
    public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);
    public delegate void ErrorCallback<in T>(string reason, T? param, Type? type, string? eventId, string? correlationId);
    public static Task<ISubscription> Subscribe<T>(this IBus busClient,
        string subjectId, 
        SubscribeCallback<T> callback,
        ErrorCallback<T>? errorCallback = null)
    {
        Task InnerCallback(object? param, Type? type, string eventId, string correlationId)
        {
            var t = (T?)param;
            return callback(t, eventId, correlationId);
        }

        void ErrorCallback(string reason, object? o, Type? type, string? eid, string? cid)
        {
            errorCallback?.Invoke(reason, default, null, eid, cid);
        }

        var clientErrorCallback = errorCallback == null
            ? (IBus.ErrorCallback?)null
            : ErrorCallback;
        return busClient.Subscribe(subjectId, typeof(T), InnerCallback, clientErrorCallback);
    }

    public static Task<IPublishResult> Publish<T>(this IBus busClient,
        string subjectId,
        T? param,
        string? eventId = null, string? correlationId = null)
    {
        if (param == null)
        {
            return busClient.Publish(subjectId, eventId, correlationId);
        }
        return busClient.Publish(subjectId, param, typeof(T), eventId, correlationId);
    }
}