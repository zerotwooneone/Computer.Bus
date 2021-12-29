namespace Computer.Bus.Contracts;

public interface IBusEvent<out T>
{
    T? Value { get; }
    string EventId { get; }
    string CorrelationId { get; }
}