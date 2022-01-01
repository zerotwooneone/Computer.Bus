using Computer.Bus.Contracts;
using Computer.Bus.RabbitMq.Client;
using Computer.Bus.RabbitMq.Contracts;
using IConnectionFactory = Computer.Bus.RabbitMq.Contracts.IConnectionFactory;

namespace Computer.Bus.RabbitMq;

public class ClientFactory
{
    public IBusClient Create(
        ISerializer serializer,
        ChannelAdapter channelAdapter)
    {
        return new BusClient(channelAdapter, serializer);
    }

    public IBusClient Create(
        ISerializer serializer,
        IConnectionFactory connectionFactory)
    {
        var clientParam = new ChannelAdapter(connectionFactory);
        return new BusClient(clientParam, serializer);
    }
}