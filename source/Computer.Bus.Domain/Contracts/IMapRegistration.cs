namespace Computer.Bus.Domain.Contracts;

public interface IMapRegistration
{
    Type Dto { get; }
    Type Domain { get; }
    Type Mapper { get; }
}