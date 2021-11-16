using System;
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
            var serializer = new Serializer();
            var listen = (new TaskFactory()).StartNew(() =>
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);

                void Callback()
                {
                    Console.WriteLine($"got a response");
                }

                var subjectId = new SubjectId { SubjectName = subjectName };
                Console.WriteLine("listening...");
                using var subscription = client.Subscribe(subjectId, Callback);

                Task.Delay(10000).Wait();
            });

            var send = (new TaskFactory()).StartNew(() =>
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create(serializer);
                var subjectId = new SubjectId { SubjectName = subjectName };
                Task.Delay(5000).Wait();
                Console.WriteLine("publishing...");
                client.Publish(subjectId);
            });
            await Task.WhenAll(listen, send);
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