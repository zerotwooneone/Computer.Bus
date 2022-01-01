using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using Computer.Bus.ProtobuffNet;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace Computer.Bus.Integration;

internal class Program
{
    private static void Main(string[] args)
    {
        var program = new Program(args);
        program.MainAsync(args).Wait();
    }

    private Program(string[] args)
    {
    }

    private async Task MainAsync(string[] args)
    {
        Console.WriteLine("Started");

        var serializer = new ProtoSerializer();
        var tests = new Test[]
        {
            new Test(()=>ParameterlessTest(serializer), nameof(ParameterlessTest)), 
            new(() => SerializeTest(serializer), nameof(SerializeTest)),
            new(() => PayloadTest(serializer), nameof(PayloadTest))
        };
        var failures = 0;
        for (var index = 0; index < tests.Length; index++)
        {
            var test = tests[index];
            try
            {
                Console.WriteLine($"Starting Test {test.name} ({index + 1}/{tests.Length})");
                await test.exec();
                Console.WriteLine($"Success - {test.name} ");
            }
            catch (Exception e)
            {
                failures++;
                Console.WriteLine(e);
                Console.WriteLine($"Test Fail - {test.name}");
            }
            finally
            {
                Console.WriteLine();
            }
        }

        Console.WriteLine($"Finished - Failed {failures}/{tests.Length}");
    }

    private async Task ParameterlessTest(ISerializer serializer)
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

                using var subscription = await client.Subscribe(subjectId, (e, c) => Callback());
                listenStarted.TrySetResult();

                await publishCompleted.Task;
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
                await listenStarted.Task;
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    await client.Publish(subjectId);
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

        await Task.WhenAll(Send(), Listen());
        Assert.AreEqual(expectedCallbacks, callbackCount);
    }

    private async Task SerializeTest(ISerializer serializer)
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

                using var subscription = await client.Subscribe<ProtoModel>(subjectId, Callback);
                listenStarted.TrySetResult();

                await publishCompleted.Task;
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
                await listenStarted.Task;
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var protoModel = new ProtoModel();
                    published.Enqueue(protoModel);
                    publishResults.Enqueue(
                        await client.Publish(subjectId, protoModel, typeof(ProtoModel))
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

        await Task.WhenAll(Send(), Listen());
        //CollectionAssert.AreEqual(published, received);
        Assert.IsTrue(publishResults.All(r => r.Success));
        Assert.AreEqual(expectedCallbacks, publishResults.Count);
    }

    private async Task PayloadTest(ISerializer serializer)
    {
        const string subjectId = "PayloadTest";
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var received = new ConcurrentQueue<Payload?>();

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer, connectionFactory);

                async Task Callback(Payload? param, string eventId, string correlationId)
                {
                    received.Enqueue(param);
                }

                using var subscription = await client.Subscribe<Payload>(subjectId, Callback);
                listenStarted.TrySetResult();

                await publishCompleted.Task;
            }
            catch (Exception e)
            {
                listenStarted.TrySetException(e);
                throw;
            }
        }

        const int expectedCallbacks = 1;
        var published = new ConcurrentQueue<Payload>();
        var publishResults = new ConcurrentQueue<IPublishResult>();

        async Task Send()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer, connectionFactory);
                await listenStarted.Task;
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var protoModel = new Payload();
                    //protoModel.Decs.Add("something", 1);
                    //protoModel.Bytes.Add("test", new byte[]{1, 2});
                    //protoModel.Strings.Add("T", "a");
                    protoModel.Bools.Add("Test", false);
                    published.Enqueue(protoModel);
                    publishResults.Enqueue(
                        await client.Publish(subjectId, protoModel, typeof(Payload))
                    );
                    Task.Delay(100).Wait();
                }

                publishCompleted.TrySetResult();
            }
            catch (Exception ex)
            {
                publishCompleted.TrySetException(ex);
                throw;
            }
        }

        await Task.WhenAll(Send(), Listen());
        CollectionAssert.AreEqual(published.SelectMany(p => p.Bools.ToArray()).ToArray(),
            received.SelectMany(p => p.Bools.ToArray()).ToArray());
        CollectionAssert.AreEqual(published.SelectMany(p => p.Ints.ToArray()).ToArray(),
            received.SelectMany(p => p.Ints.ToArray()).ToArray());
        CollectionAssert.AreEqual(published.SelectMany(p => p.Decs.ToArray()).ToArray(),
            received.SelectMany(p => p.Decs.ToArray()).ToArray());
        CollectionAssert.AreEqual(published.SelectMany(p => p.Strings.ToArray()).ToArray(),
            received.SelectMany(p => p.Strings.ToArray()).ToArray());
        CollectionAssert.AreEqual(published.SelectMany(p => p.Bytes.ToArray()).ToArray(),
            received.SelectMany(p => p.Bytes.ToArray()).ToArray());

        Assert.IsTrue(publishResults.All(r => r.Success));
        Assert.AreEqual(expectedCallbacks, publishResults.Count);
    }

    public record Test(Func<Task> exec, string name);

    [ProtoContract]
    public record Payload
    {
        [ProtoMember(1)] public Dictionary<string, int>? Ints { get; init; } = new();
        [ProtoMember(2)] public Dictionary<string, string>? Strings { get; init; } = new();
        [ProtoMember(3)] public Dictionary<string, bool>? Bools { get; init; } = new();
        [ProtoMember(4)] public Dictionary<string, decimal>? Decs { get; init; } = new();
        [ProtoMember(5)] public Dictionary<string, byte[]>? Bytes { get; init; } = new();
    }
}