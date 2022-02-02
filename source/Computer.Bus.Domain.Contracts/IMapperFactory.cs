using Computer.Bus.Domain.Contracts.Models;

namespace Computer.Bus.Domain.Contracts;

public interface IMapperFactory
{
    IMapper? GetMapper(Type mapperType, Type dto, Type domain);
}