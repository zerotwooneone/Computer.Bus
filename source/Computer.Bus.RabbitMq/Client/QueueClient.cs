using System;
using System.Text;
using Computer.Bus.Contracts.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Computer.Bus.RabbitMq.Client
{
    public class QueueClient 
    {
        public PublishResult Publish(string subjectId)
        {
            //todo: remove this dummy message
            string message = "Hello World!";
            var body = Encoding.UTF8.GetBytes(message);

            return Publish(subjectId, body);
        }
        
        public PublishResult Publish(string subjectId, byte[] body)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: subjectId,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                    routingKey: "hello",
                    basicProperties: null,
                    body: body);
            }

            return new PublishResult();
        }
        
        public ISubscription Subscribe(string subjectId, Action callback)
        {
            return Subscribe(subjectId, (s) =>
            {
                var message = Encoding.UTF8.GetString(s);
                Console.WriteLine($"received {message}");
                callback();
            });
        }
        
        public ISubscription Subscribe(string subjectId, 
            Action<byte[]> callback)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: subjectId,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(channel);

            void OnConsumerOnReceived(object? model, BasicDeliverEventArgs ea)
            {
                callback(ea.Body.ToArray());
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }

            consumer.Received += OnConsumerOnReceived;
            channel.BasicConsume(queue: subjectId,
                autoAck: false,
                consumer: consumer);
            return new RabbitSubscription
            {
                Unsubscribe = () =>
                {
                    consumer.Received -= OnConsumerOnReceived;
                    channel.Dispose();
                    connection.Dispose();
                }
            };
        }
    }
}