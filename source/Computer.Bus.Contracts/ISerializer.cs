using System.Threading.Tasks;

namespace Computer.Bus.Contracts;

public interface ISerializer
{
    Task<byte[]> Serialize(string eventId, string correlationId);

    Task<byte[]> Serialize<T>(T? param,
        string eventId, string correlationId);

    Task<IBusEvent<T>?> Deserialize<T>(byte[] bytes);
}