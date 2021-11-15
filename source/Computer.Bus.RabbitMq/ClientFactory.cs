using System;
using Computer.Bus.Contracts;
using Computer.Bus.RabbitMq.Client;

namespace Computer.Bus.RabbitMq
{
    public class ClientFactory
    {
        public IBusClient Create()
        {
            return new BusClient();
        }
    }
}