using Computer.Bus.Contracts;
using Computer.Bus.RabbitMq.Client;
using Computer.Bus.RabbitMq.Serialize;

namespace Computer.Bus.RabbitMq
{
    public class ClientFactory
    {
        public IBusClient Create(
            ISerializer serializer,
            QueueClient? queueClient = null)
        {
            var clientParam = queueClient ?? new QueueClient();
            return new BusClient(clientParam, serializer);
        }
    }
}