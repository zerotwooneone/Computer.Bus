using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Computer.Bus.RabbitMq.Contracts;

public interface IConnectionFactory
{
    Task<IConnection> GetConnection(string connectionId, string? subjectId = null);
}