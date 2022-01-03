using Computer.Bus.Domain.Contracts;
using System;

namespace publishDtoNameSpace;
public partial class ExampleClassMapper : IMapper
{
    public object? DtoToDomain(Type dtoType, object obj, Type domainType)
    {
        if (!dtoType.IsAssignableFrom(typeof(publishDtoNameSpace.ExampleClass)) || !domainType.IsAssignableFrom(typeof(publishDomainNameSpace.ExampleClass)) || obj == null)
        {
            return null;
        }

        var dto = (publishDtoNameSpace.ExampleClass)obj;
        return new publishDomainNameSpace.ExampleClass{Test = dto.Test, SomeOtherTest = dto.SomeOtherTest};
    }

    public object? DomainToDto(Type domainType, object obj, Type dtoType)
    {
        if (!dtoType.IsAssignableFrom(typeof(publishDtoNameSpace.ExampleClass)) || !domainType.IsAssignableFrom(typeof(publishDomainNameSpace.ExampleClass)) || obj == null)
        {
            return null;
        }

        var domain = (publishDomainNameSpace.ExampleClass)obj;
        return new publishDtoNameSpace.ExampleClass{Test = domain.Test, SomeOtherTest = domain.SomeOtherTest};
    }
}