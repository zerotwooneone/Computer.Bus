using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Computer.Bus.Integration;

public class SingletonConnectionFactory : RabbitMq.Contracts.IConnectionFactory
{
    private static readonly Lazy<ConnectionFactory> ConnectionFactory = new(() => new ConnectionFactory()
        { HostName = "localhost", DispatchConsumersAsync = true });

    private static readonly Lazy<IConnection> Connection =
        new(() => ConnectionFactory.Value.CreateConnection());

    public Task<IConnection> GetConnection(string connectionId, string? subjectId = null)
    {
        return Task.FromResult(Connection.Value);
    }
}