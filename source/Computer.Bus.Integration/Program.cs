using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace Computer.Bus.Integration
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = new Program(args);
            program.MainAsync(args).Wait();
        }

        private Program(string[] args) { }

        private async Task MainAsync(string[] args)
        {
            Console.WriteLine("Started");
            
            var serializer = new ProtoSerializer();
            IEnumerable<Func<Task>> tests = new []
            {
                ()=>ParameterlessTest(serializer), 
                ()=>SerializeTest(serializer),
                ()=>PayloadTest(serializer)
            };
            foreach (var test in tests)
            {
                try
                {
                    Console.WriteLine($"Test Starting");
                    await test();
                    Console.WriteLine($"Test Success");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine($"Test Fail !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                }
                finally
                {
                    Console.WriteLine();
                }
            }
            
            Console.WriteLine("Finished");
        }

        private async Task ParameterlessTest(ISerializer serializer)
        {
            const string subjectName = "ParameterlessTest";
            var subjectId = new SubjectId { SubjectName = subjectName };
            var listenStarted = new TaskCompletionSource();
            var publishCompleted = new TaskCompletionSource();
            var callbackCount = 0;
            async Task Listen()
            {
                try
                {
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);

                    void Callback()
                    {
                        callbackCount++;
                    }
                   
                    using var subscription = client.Subscribe(subjectId, Callback);
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
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);
                    await listenStarted.Task;
                    for (int pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                    {
                        client.Publish(subjectId);
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
            const string subjectName = "SerializeTest";
            var subjectId = new SubjectId { SubjectName = subjectName };
            var listenStarted = new TaskCompletionSource();
            var publishCompleted = new TaskCompletionSource();
            var received = new ConcurrentQueue<ProtoModel>();
            async Task Listen()
            {
                try
                {
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);

                    void Callback(ProtoModel p)
                    {
                        received.Enqueue(p);
                    }
                   
                    using var subscription = client.Subscribe<ProtoModel>(subjectId, Callback);
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
            async Task Send()
            {
                try
                {
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);
                    await listenStarted.Task;
                    for (int pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                    {
                        var protoModel = new ProtoModel();
                        published.Enqueue(protoModel);
                        client.Publish(subjectId, protoModel);
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
            CollectionAssert.AreEqual(published, received);
        }
        
        private async Task PayloadTest(ISerializer serializer)
        {
            const string subjectName = "PayloadTest";
            var subjectId = new SubjectId { SubjectName = subjectName };
            var listenStarted = new TaskCompletionSource();
            var publishCompleted = new TaskCompletionSource();
            var received = new ConcurrentQueue<Payload>();
            async Task Listen()
            {
                try
                {
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);

                    void Callback(Payload p)
                    {
                        received.Enqueue(p);
                    }
                   
                    using var subscription = client.Subscribe<Payload>(subjectId, Callback);
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
            async Task Send()
            {
                try
                {
                    var clientFactory = new ClientFactory();
                    var client = clientFactory.Create(serializer);
                    await listenStarted.Task;
                    for (int pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                    {
                        var protoModel = new Payload();
                        //protoModel.Decs.Add("something", 1);
                        //protoModel.Bytes.Add("test", new byte[]{1, 2});
                        //protoModel.Strings.Add("T", "a");
                        protoModel.Bools.Add("Test", false);
                        published.Enqueue(protoModel);
                        client.Publish(subjectId, protoModel);
                        Task.Delay(100).Wait();
                    }

                    publishCompleted.TrySetResult();
                }
                catch(Exception ex)
                {
                    publishCompleted.TrySetException(ex);
                    throw;
                }
            }
            await Task.WhenAll(Send(), Listen());
            //CollectionAssert.AreEqual(published, received);
        }

        [ProtoContract]
        public record Payload
        {
            [ProtoMember(1)]
            public Dictionary<string, int>? Ints { get; init; } = new Dictionary<string, int>();
            [ProtoMember(2)]
            public Dictionary<string, string>? Strings { get; init; } = new Dictionary<string, string>();
            [ProtoMember(3)]
            public Dictionary<string, bool>? Bools { get; init; } = new Dictionary<string, bool>();
            [ProtoMember(4)]
            public Dictionary<string, Decimal>? Decs { get; init; } = new Dictionary<string, Decimal>();
            [ProtoMember(5)]
            public Dictionary<string, byte[]>? Bytes { get; init; } = new Dictionary<string, byte[]>();
        }
    }
}