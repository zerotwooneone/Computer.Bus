namespace Computer.Bus.RabbitMq.Contracts;

public interface IBusEvent
{
    object? Payload { get; }
    string EventId { get; }
    string CorrelationId { get; }
}

public interface IBusEvent<T>
{
    T Payload { get; }
    string EventId { get; }
    string CorrelationId { get; }
}