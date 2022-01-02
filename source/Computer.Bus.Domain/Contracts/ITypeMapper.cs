namespace Computer.Bus.Domain.Contracts;

public interface IMapperFactory
{
    IMapper GetMapper(Type mapperType, Type dto, Type domain);
}