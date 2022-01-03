using Computer.Bus.Domain.Contracts;
using System;

namespace subscribeDtoNameSpace;
public partial class ExampleClassMapper : IMapper
{
    public object? DtoToDomain(Type dtoType, object obj, Type domainType)
    {
        if (!dtoType.IsAssignableFrom(typeof(subscribeDtoNameSpace.ExampleClass)) || !domainType.IsAssignableFrom(typeof(subscribeDomainNameSpace.ExampleClass)) || obj == null)
        {
            return null;
        }

        var dto = (subscribeDtoNameSpace.ExampleClass)obj;
        return new subscribeDomainNameSpace.ExampleClass{Test = dto.Test, SomeOtherTest = dto.SomeOtherTest};
    }

    public object? DomainToDto(Type domainType, object obj, Type dtoType)
    {
        if (!dtoType.IsAssignableFrom(typeof(subscribeDtoNameSpace.ExampleClass)) || !domainType.IsAssignableFrom(typeof(subscribeDomainNameSpace.ExampleClass)) || obj == null)
        {
            return null;
        }

        var domain = (subscribeDomainNameSpace.ExampleClass)obj;
        return new subscribeDtoNameSpace.ExampleClass{Test = domain.Test, SomeOtherTest = domain.SomeOtherTest};
    }
}