using System;
using System.Text;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Computer.Bus.RabbitMq.Client
{
    public class BusClient : IBusClient
    {
        public PublishResult Publish(ISubjectId subjectId)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                    routingKey: "hello",
                    basicProperties: null,
                    body: body);
            }

            return new PublishResult();
        }
        
        public PublishResult Publish<T>(ISubjectId subjectId, T param)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe(ISubjectId subjectId, Action callback)
        {
            throw new NotImplementedException();
        }

        public ISubscription Subscribe<T>(ISubjectId subjectId, Action<T> callback)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                void OnConsumerOnReceived(object? model, BasicDeliverEventArgs ea)
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received {0}", message);
                }

                consumer.Received += OnConsumerOnReceived;
                channel.BasicConsume(queue: "hello",
                    autoAck: true,
                    consumer: consumer);
                return new RabbitSubscription
                {
                    Unsubscribe = () =>
                    {
                        consumer.Received -= OnConsumerOnReceived;
                    }
                };
            }
        }
    }
}