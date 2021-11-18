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
            IEnumerable<Func<Task>> tests = new []{()=>ParameterlessTest(serializer), ()=>SerializeTest(serializer)};
            foreach (var test in tests)
            {
                try
                {
                    await test();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            await ParameterlessTest(serializer);
            await SerializeTest(serializer);
            
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
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);

                void Callback()
                {
                    callbackCount++;
                }
               
                using var subscription = client.Subscribe(subjectId, Callback);
                listenStarted.TrySetResult();

                try
                {
                    await publishCompleted.Task;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"exception:{e}");
                }
            }
            const int expectedCallbacks = 50;
            async Task Send()
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
            await Task.WhenAll(Send(), Listen());
            Assert.AreEqual(expectedCallbacks, callbackCount);
        }
        
        private async Task SerializeTest(ISerializer serializer)
        {
            const string subjectName = "ParameterlessTest";
            var subjectId = new SubjectId { SubjectName = subjectName };
            var listenStarted = new TaskCompletionSource();
            var publishCompleted = new TaskCompletionSource();
            var received = new ConcurrentQueue<ProtoModel>();
            async Task Listen()
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);

                void Callback(ProtoModel p)
                {
                    received.Enqueue(p);
                }
               
                using var subscription = client.Subscribe<ProtoModel>(subjectId, Callback);
                listenStarted.TrySetResult();

                try
                {
                    await publishCompleted.Task;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"exception:{e}");
                }
            }
            const int expectedCallbacks = 50;
            var published = new ConcurrentQueue<ProtoModel>();
            async Task Send()
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
            await Task.WhenAll(Send(), Listen());
            CollectionAssert.AreEqual(published, received);
        }
    }
}