namespace Computer.Bus.RabbitMq.Serialize
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] bytes);
    }
}