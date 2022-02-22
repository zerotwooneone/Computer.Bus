using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using Computer.Bus.ProtobuffNet;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Contracts;
using NUnit.Framework;

namespace Computer.Bus.Integration;

public class BusClientTests
{
    private ISerializer serializer;
    [SetUp]
    public void Setup()
    {
        serializer = new ProtoSerializer();
    }

    [Test]
    public async Task Parameterless()
    {
        const string subjectId = "ParameterlessTest";
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var callbackCount = 0;

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer, connectionFactory);

                async Task Callback()
                {
                    callbackCount++;
                }

                void ErrorCallback(string reason, Type? type, string? id, string? correlationId, object? o)
                {
                    Assert.Fail($"error callback: {reason}");
                }

                using var subscription = await client.Subscribe(subjectId, 
                    (e, c) => Callback(),
                    ErrorCallback).ConfigureAwait(false);
                listenStarted.TrySetResult();

                await publishCompleted.Task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                listenStarted.TrySetException(e);
                throw;
            }
        }

        const int expectedCallbacks = 50;

        async Task Send()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer, connectionFactory);
                await listenStarted.Task.ConfigureAwait(false);
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    await client.Publish(subjectId).ConfigureAwait(false);
                    Task.Delay(100).Wait();
                }

                publishCompleted.TrySetResult();
            }
            catch (Exception e)
            {
                publishCompleted.TrySetException(e);
                throw;
            }
        }

        await Task.WhenAll(Send(), Listen()).ConfigureAwait(false);
        Assert.AreEqual(expectedCallbacks, callbackCount);
    }

    [Test]
    public async Task Serialize()
    {
        const string subjectId = "SerializeTest";
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var received = new ConcurrentQueue<ProtoModel?>();

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer, connectionFactory);

                async Task Callback(ProtoModel? param, string eventId, string correlationId)
                {
                    received.Enqueue(param);
                }
                
                void ErrorCallback(string reason, string? id, string? correlationId, object? o)
                {
                    Assert.Fail($"error callback: {reason}");
                }

                using var subscription = await client.Subscribe<ProtoModel>(subjectId, Callback, ErrorCallback).ConfigureAwait(false);
                listenStarted.TrySetResult();

                await publishCompleted.Task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                listenStarted.TrySetException(e);
                throw;
            }
        }

        const int expectedCallbacks = 50;
        var published = new ConcurrentQueue<ProtoModel>();
        var publishResults = new ConcurrentQueue<IPublishResult>();

        async Task Send()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client =clientFactory.Create(serializer, connectionFactory);
                await listenStarted.Task.ConfigureAwait(false);
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var protoModel = new ProtoModel();
                    published.Enqueue(protoModel);
                    publishResults.Enqueue(
                        await client.Publish(subjectId, protoModel, typeof(ProtoModel)).ConfigureAwait(false)
                    );
                    Task.Delay(100).Wait();
                }

                publishCompleted.TrySetResult();
            }
            catch (Exception e)
            {
                publishCompleted.TrySetException(e);
                throw;
            }
        }

        await Task.WhenAll(Send(), Listen()).ConfigureAwait(false);
        //CollectionAssert.AreEqual(published, received);
        Assert.IsTrue(publishResults.All(r => r.Success));
        Assert.AreEqual(expectedCallbacks, publishResults.Count);
    }
}