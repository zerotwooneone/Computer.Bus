using System;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;
using Computer.Bus.RabbitMq;

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
            Console.WriteLine("Hello World!");
            const string subjectName = "Hello";
            var listen = (new TaskFactory()).StartNew(() =>
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create();

                void Callback(string s)
                {
                    Console.WriteLine($"got a response {s}");
                }

                var subjectId = new SubjectId { SubjectName = subjectName };
                Console.WriteLine("listening...");
                var subscription = client.Subscribe<string>(subjectId, Callback);

                Task.Delay(10000).Wait();
            });

            var send = (new TaskFactory()).StartNew(() =>
            {
                var clientFactory = new ClientFactory();
                var client = clientFactory.Create();
                var subjectId = new SubjectId { SubjectName = subjectName };
                Task.Delay(100).Wait();
                Console.WriteLine("publishing...");
                client.Publish(subjectId);
            });
            await Task.WhenAll(listen, send);
        }
    }
}