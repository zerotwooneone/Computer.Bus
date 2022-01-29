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

    Task<ISubscription> Subscribe(string subjectId, Type type, SubscribeCallbackP callback);
    Task<ISubscription> Subscribe(string subjectId, SubscribeCallbackNp callback);
}

public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);
public delegate Task SubscribeCallbackP(object? param, Type? type, string eventId, string correlationId);
public delegate Task SubscribeCallbackNp(string eventId, string correlationId);

public static class BusClientExtensions {
    public static Task<ISubscription> Subscribe<T>(this IBusClient busClient,
        string subjectId, SubscribeCallback<T> callback)
    {
        Task innerCallback(object? param, Type? type, string eventId, string correlationId)
        {
            var t = (T?)param;
            return callback(t, eventId, correlationId);
        }
        return busClient.Subscribe(subjectId, typeof(T), innerCallback);
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