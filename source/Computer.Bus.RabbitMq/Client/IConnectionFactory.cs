using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Computer.Bus.RabbitMq.Client;

public interface IConnectionFactory
{
    Task<IConnection> GetConnection(string connectionId, string? subjectId = null);
}