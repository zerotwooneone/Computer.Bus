using System;
using System.Text;
using System.Threading.Channels;
using Computer.Bus.Contracts.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Computer.Bus.RabbitMq.Client
{
    public class QueueClient
    {
        private static readonly Lazy<ConnectionFactory> _connectionFactory = new(() => new ConnectionFactory() { HostName = "localhost" });
        private static readonly Lazy<IConnection> _connection =
            new(() => _connectionFactory.Value.CreateConnection());

        private const string BusExchange = "computer.BusExchange";
        public PublishResult Publish(string subjectId)
        {
            //todo: remove this dummy message
            string message = $"Hello World! {DateTime.Now:mm:ss.ffff}";
            Console.WriteLine($"publish message {message}");
            var body = Encoding.UTF8.GetBytes(message);

            return Publish(subjectId, body);
        }
        
        public PublishResult Publish(string subjectId, byte[] body)
        {
            //var factory = new ConnectionFactory() { HostName = "localhost" };
            //using var connection = factory.CreateConnection();
            using var channel = _connection.Value.CreateModel();
            CreateBusExchange(channel);
            // channel.QueueDeclare(queue: subjectId,
            //     durable: true,
            //     exclusive: false,
            //     autoDelete: false,
            //     arguments: null);

            // var properties = channel.CreateBasicProperties();
            // properties.Persistent = true;

            channel.BasicPublish(exchange: BusExchange,
                routingKey: GetBusRoutingKey(subjectId),
                basicProperties: null,
                body: body);

            return new PublishResult();
        }

        private static void CreateBusExchange(IModel? channel)
        {
            channel.ExchangeDeclare(exchange: BusExchange, type: ExchangeType.Direct);
        }

        private string GetBusRoutingKey(string subjectId)
        {
            return $"computer.bus.subject.{subjectId}";
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
            //var factory = new ConnectionFactory() { HostName = "localhost" };
            //using var connection = factory.CreateConnection();
            var channel = _connection.Value.CreateModel();
            
            CreateBusExchange(channel);
            
            // channel.QueueDeclare(queue: subjectId,
            //     durable: true,
            //     exclusive: false,
            //     autoDelete: false,
            //     arguments: null);
            var queueName = channel.QueueDeclare(queue: subjectId, durable: true).QueueName;
            channel.QueueBind(queue: queueName,
                exchange: BusExchange,
                routingKey: GetBusRoutingKey(subjectId));

            var consumer = new EventingBasicConsumer(channel);

            void OnConsumerOnReceived(object? model, BasicDeliverEventArgs ea)
            {
                callback(ea.Body.ToArray());
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }

            consumer.Received += OnConsumerOnReceived;
            channel.BasicConsume(queue: queueName,
                autoAck: false,
                consumer: consumer);
            return new RabbitSubscription
            {
                Unsubscribe = () =>
                {
                    Console.WriteLine("unsubscribing...");
                    consumer.Received -= OnConsumerOnReceived;
                    channel.Dispose();
                    //connection.Dispose();
                }
            };
        }
    }
}