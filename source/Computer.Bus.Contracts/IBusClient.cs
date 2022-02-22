using System;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.Contracts;

public interface IBusClient
{
    Task<IPublishResult> Publish(string subjectId,
        string? eventId = null, string? correlationId = null);

    Task<IPublishResult> Publish(string subjectId,
        object? param, Type type,
        string? eventId = null, string? correlationId = null);

    Task<ISubscription> Subscribe(string subjectId, Type type, SubscribeCallbackP callback, ErrorCallback? errorCallback = null);
    Task<ISubscription> Subscribe(string subjectId, SubscribeCallbackNp callback, ErrorCallback? errorCallback = null);
    public delegate Task SubscribeCallbackP(object? param, Type? type, string eventId, string correlationId);
    public delegate Task SubscribeCallbackNp(string eventId, string correlationId);
    public delegate void ErrorCallback(string reason, Type? type, string? eventId, string? correlationId, object? param = null);
}
public static class BusClientExtensions {
    public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);
    public delegate void ErrorCallback<in T>(string reason, string? eventId, string? correlationId, T? param = default);
    public static Task<ISubscription> Subscribe<T>(this IBusClient busClient,
        string subjectId, 
        SubscribeCallback<T> callback,
        ErrorCallback<T>? errorCallback = null)
    {
        Task InnerCallback(object? param, Type? type, string eventId, string correlationId)
        {
            var t = (T?)param;
            return callback(t, eventId, correlationId);
        }

        void InnerErrorCallback(string reason, Type? type, string? eId, string? cId, object? param)
        {
            T? p;
            try
            {
                p = (T?)param;
            }
            catch
            {
                p = default;
            }

            errorCallback(reason, eId, cId, p);
        }

        var innerErrorCallback = errorCallback == null
            ? (IBusClient.ErrorCallback?)null
            : InnerErrorCallback;
        return busClient.Subscribe(subjectId, typeof(T), InnerCallback, innerErrorCallback);
    }

    public static Task<IPublishResult> Publish<T>(this IBusClient busClient,
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