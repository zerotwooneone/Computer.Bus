using System;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;

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
        var body = await _serializer.Serialize(eid, cid);
        return await _channelAdapter.Publish(subjectId, body);
    }

    public async Task<IPublishResult> Publish<T>(string subjectId,
        T? param,
        string? eventId = null, string? correlationId = null)
    {
        var eid = eventId ?? Guid.NewGuid().ToString();
        var cid = correlationId ?? Guid.NewGuid().ToString();
        var body = await _serializer.Serialize(param, eid, cid);
        return await _channelAdapter.Publish(subjectId, body);
    }

    public Task<ISubscription> Subscribe<T>(string subjectId, SubscribeCallback<T> callback)
    {
        async Task InnerCallback(byte[] b)
        {
            var @event = await _serializer.Deserialize<T>(b);
            if (@event == null)
            {
                return;
            }
            await callback(@event.Value, @event.EventId, @event.CorrelationId);
        }

        return _channelAdapter.Subscribe(subjectId, InnerCallback);
    }
}