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
        ISerializationResult<byte[]?> result;
        //try
        //{
            result = await _serializer.Serialize(eid, cid).ConfigureAwait(false);
        //}
        //catch (Exception e) //todo: catch only serialization exception
        //{
        //    return PublishResult.CreateError(e.ToString());
        //}
        if (!result.Success || result.Param == null)
        {
            return PublishResult.CreateError("Something went wrong while serializing");
        }

        try
        {
            return await _channelAdapter.Publish(subjectId, result.Param).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return PublishResult.CreateError(e.ToString());
        }
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
            ISerializationResult<byte[]?> result;
            //try
            //{
                result = await _serializer.Serialize(param, type, eid, cid).ConfigureAwait(false);
            //}
            //catch (Exception e)
            //{
            //    return PublishResult.CreateError(e.ToString());
            //}
            if (!result.Success || result.Param == null)
            {
                return PublishResult.CreateError("Serialization failed");
            }
            body = result.Param;
        }

        try
        {
            return await _channelAdapter.Publish(subjectId, body).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return PublishResult.CreateError(e.ToString());
        }
    }
    
    public Task<ISubscription> Subscribe(string subjectId, Type type, 
        IBusClient.SubscribeCallbackP callback,
        IBusClient.ErrorCallback? errorCallback = null)
    {
        async Task InnerCallback(byte[] b)
        {
            ISerializationResult<IBusEvent> result;
            try
            {
                result = await _serializer.Deserialize(b, type).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(e.ToString(), type, null, null);
                return;
            }
            if (!result.Success || result.Param == null)
            {
                errorCallback?.Invoke(result.Reason ?? "bus deserialization failed, but there was no reason", 
                    type, null, null, result.Param);
                return;
            }
            await callback(result.Param.Payload, type, result.Param.EventId, result.Param.CorrelationId).ConfigureAwait(false);
        }

        var innerChannelErrorCallback = errorCallback == null
            ? (Action<string, string>?)null
            : (string sid, string reason) =>
            {
                errorCallback.Invoke(reason, null, sid, null, null);
            };
        
        return _channelAdapter.Subscribe(subjectId, InnerCallback, innerChannelErrorCallback);
    }
    
    public Task<ISubscription> Subscribe(string subjectId, 
        IBusClient.SubscribeCallbackNp callback,
        IBusClient.ErrorCallback? errorCallback = null)
    {
        async Task InnerCallback(byte[] b)
        {
            ISerializationResult<IBusEvent> result;
            try
            {
                result = await _serializer.Deserialize(b).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(e.ToString(),
                    null, null, null, null);
                return;
            }
            if (!result.Success || result.Param == null)
            {
                errorCallback?.Invoke(result.Reason ?? "bus deserialization failed, but there was no reason",
                    null, null, null, result.Param);
                return;
            }
            await callback(result.Param.EventId, result.Param.CorrelationId).ConfigureAwait(false);
        }
        
        var innerChannelErrorCallback = errorCallback == null
            ? (Action<string, string>?)null
            : (string sid, string reason) =>
            {
                errorCallback.Invoke(reason, null, sid, null, null);
            };

        return _channelAdapter.Subscribe(subjectId, InnerCallback, innerChannelErrorCallback);
    }
}