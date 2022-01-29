using System;
using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IConnectionFactory = Computer.Bus.RabbitMq.Contracts.IConnectionFactory;

namespace Computer.Bus.RabbitMq.Client;

public class ChannelAdapter
{
    private readonly IConnectionFactory _connectionFactory;
    private const string BusExchange = "computer.BusExchange";

    public ChannelAdapter(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PublishResult> Publish(string subjectId, byte[]? body)
    {
        var connection = await _connectionFactory.GetConnection("default", subjectId).ConfigureAwait(false);
        using var channel = connection.CreateModel();
        CreateBusExchange(channel);

        channel.BasicPublish(BusExchange,
            GetBusRoutingKey(subjectId),
            null,
            body);

        return PublishResult.SuccessResult;
    }

    private static void CreateBusExchange(IModel? channel)
    {
        channel.ExchangeDeclare(BusExchange, ExchangeType.Direct);
    }

    private static string GetBusRoutingKey(string subjectId)
    {
        var sanitized = SanitizeSubjectForRouting(subjectId);
        return $"computer.bus.subject.{sanitized}";
    }

    private static string SanitizeSubjectForRouting(string subjectId)
    {
        const string replaceChar = "%";
        return subjectId
            .Replace(replaceChar, $"{replaceChar}37")
            .Replace(".", $"{replaceChar}46");
    }

    public async Task<ISubscription> Subscribe(string subjectId,
        Func<byte[], Task> callback)
    {
        var connection = await _connectionFactory.GetConnection("default", subjectId).ConfigureAwait(false);
        var channel = connection.CreateModel();

        CreateBusExchange(channel);

        var queueName = channel.QueueDeclare(
            durable: true,
            autoDelete: true).QueueName;
        channel.QueueBind(queueName,
            BusExchange,
            GetBusRoutingKey(subjectId));

        var consumer = new AsyncEventingBasicConsumer(channel);

        async Task OnConsumerOnReceived(object? model, BasicDeliverEventArgs ea)
        {
            try
            {
                await callback(ea.Body.ToArray()).ConfigureAwait(false);
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception e)
            {
                //todo: do something when an exception occurs
                Console.WriteLine(e);
                throw;
            }
        }

        consumer.Received += OnConsumerOnReceived;
        channel.BasicConsume(queueName,
            false,
            consumer);
        return new RabbitSubscription
        {
            Unsubscribe = () =>
            {
                consumer.Received -= OnConsumerOnReceived;
                channel.Dispose();
            }
        };
    }
}