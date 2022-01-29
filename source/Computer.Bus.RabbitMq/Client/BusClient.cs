using System;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using Computer.Bus.RabbitMq.Contracts;

namespace Computer.Bus.RabbitMq.Client;

public class BusClient : IBusClient
{
    private readonly ChannelAdapter _channelAdapter;
    private readonly ISerializer _serializer;

    public BusClient(
        ChannelAdapter channelAdapter,
        ISerializer serializer)
    {
        _channelAdapter = channelAdapter;
        _serializer = serializer;
    }

    public async Task<IPublishResult> Publish(string subjectId,
        string? eventId = null, string? correlationId = null)
    {
        var eid = eventId ?? Guid.NewGuid().ToString();
        var cid = correlationId ?? Guid.NewGuid().ToString();
        var body = await _serializer.Serialize(eid, cid).ConfigureAwait(false);
        if (body == null)
        {
            return PublishResult.CreateError("Something went wrong while serializing");
        }
        return await _channelAdapter.Publish(subjectId, body).ConfigureAwait(false);
    }

    public async Task<IPublishResult> Publish(string subjectId,
        object? param, Type type,
        string? eventId = null, string? correlationId = null)
    {
        var eid = eventId ?? Guid.NewGuid().ToString();
        var cid = correlationId ?? Guid.NewGuid().ToString();
        byte[]? body;
        if (param == null)
        {
            body = null;
        }
        else
        {
            body = await _serializer.Serialize(param, type, eid, cid).ConfigureAwait(false);
            if (body == null)
            {
                return PublishResult.CreateError("Serialization failed");
            }
        }
        return await _channelAdapter.Publish(subjectId, body).ConfigureAwait(false);
    }
    // public async Task<IPublishResult> Publish<T>(string subjectId,
    //     T? param,
    //     string? eventId = null, string? correlationId = null)
    // {
    //     var eid = eventId ?? Guid.NewGuid().ToString();
    //     var cid = correlationId ?? Guid.NewGuid().ToString();
    //     var body = await _serializer.Serialize(param, eid, cid);
    //     return await _channelAdapter.Publish(subjectId, body);
    // }
    
    public Task<ISubscription> Subscribe(string subjectId, Type type, SubscribeCallbackP callback)
    {
        async Task InnerCallback(byte[] b)
        {
            var @event = await _serializer.Deserialize(b, type).ConfigureAwait(false);
            if (@event == null)
            {
                return;
            }
            await callback(@event.Payload, type, @event.EventId, @event.CorrelationId).ConfigureAwait(false);
        }

        return _channelAdapter.Subscribe(subjectId, InnerCallback);
    }
    
    public Task<ISubscription> Subscribe(string subjectId,SubscribeCallbackNp callback)
    {
        async Task InnerCallback(byte[] b)
        {
            var @event = await _serializer.Deserialize(b).ConfigureAwait(false);
            if (@event == null)
            {
                return;
            }
            await callback(@event.EventId, @event.CorrelationId).ConfigureAwait(false);
        }

        return _channelAdapter.Subscribe(subjectId, InnerCallback);
    }
}