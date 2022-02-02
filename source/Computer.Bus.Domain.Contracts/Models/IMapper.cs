namespace Computer.Bus.Domain.Contracts.Models;

public interface IMapper
{
    object? DtoToDomain(Type dtoType, object dto, Type domainType);
    object? DomainToDto(Type domainType, object domain, Type dtoType);
}