using System;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Serialize;

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
            const string subjectName = "Hello";
            var subjectId = new SubjectId { SubjectName = subjectName };
            var serializer = new Serializer();
            async Task Listen()
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);

                void Callback()
                {
                    //Console.WriteLine("inside callback");
                }

                
                Console.WriteLine("listening...");
                using var subscription = client.Subscribe(subjectId, Callback);

                //Task.Delay(10000).Wait();
                try
                {
                    await Task.Delay(10000);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"exception:{e}");
                }
            }

            var send = (new TaskFactory()).StartNew(() =>
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);
                for (int pubCount = 1; pubCount < 50; pubCount++)
                {
                    //Console.WriteLine("publishing...");
                    client.Publish(subjectId);
                    Task.Delay(100).Wait();
                }
            });
            await Task.WhenAll(send, Listen());
        }
    }

    internal class Serializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}