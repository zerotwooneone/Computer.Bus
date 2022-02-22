using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using Computer.Bus.ProtobuffNet;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Client;
using Computer.Bus.RabbitMq.Contracts;
using NUnit.Framework;

namespace Computer.Bus.Integration;

public class RequestTests
{
    private ISerializer _serializer;
    [SetUp]
    public void Setup()
    {
        _serializer = new ProtoSerializer();
    }

    [Test]
    public async Task Test1()
    {
        const string subjectId = "SerializeTest";
        const string responseSubject = "responseSubject";
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var received = new ConcurrentQueue<ProtoModel?>();

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client2 = clientFactory.Create(_serializer, connectionFactory);
                var requestService = new RequestService(client2);

                Task<ProtoModel?> Callback(ProtoModel? param, string eventId, string correlationId)
                {
                    received.Enqueue(param);
                    return Task.FromResult<ProtoModel?>(new ProtoModel
                        { fNumber = received.Count, someString = "some response", Timestamp = DateTime.Now });
                }

                void ErrorCallback(string reason, ProtoModel? model, string? id, string? correlationId)
                {
                    Assert.Fail(reason);
                }

                using var subscription = requestService.Listen<ProtoModel, ProtoModel>(subjectId, responseSubject, 
                    Callback, ErrorCallback);
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
        var publishResults = new ConcurrentQueue<IResponse<ProtoModel>>();

        async Task Send()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client2 = clientFactory.Create(_serializer, connectionFactory);
                var requestService = new RequestService(client2);
                await listenStarted.Task.ConfigureAwait(false);
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var protoModel = new ProtoModel();
                    published.Enqueue(protoModel);
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    publishResults.Enqueue(
                        await requestService.Request<ProtoModel, ProtoModel>(
                            protoModel,
                            subjectId,
                            responseSubject,
                            cancellationToken: cts.Token).ConfigureAwait(false)
                    );
                    await Task.Delay(100, cts.Token).WaitAsync(cts.Token).ConfigureAwait(false);
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