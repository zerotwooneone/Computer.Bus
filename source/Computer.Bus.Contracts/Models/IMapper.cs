using System;

namespace Computer.Bus.Domain.Contracts;

public interface IMapper
{
    object? DtoToDomain(Type dtoType, object dto, Type domainType);
    object? DomainToDto(Type domainType, object domain, Type dtoType);
}