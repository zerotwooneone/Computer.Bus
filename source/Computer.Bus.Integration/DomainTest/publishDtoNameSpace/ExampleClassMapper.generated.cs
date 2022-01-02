namespace publishDtoNameSpace;
public partial class ExampleClassMapper
{
    public publishDomainNameSpace.ExampleClass DtoToDomain(publishDtoNameSpace.ExampleClass dto)
    {
        return new publishDomainNameSpace.ExampleClass{Test = dto.Test, SomeOtherTest = dto.SomeOtherTest};
    }

    public publishDtoNameSpace.ExampleClass DomainToDto(publishDomainNameSpace.ExampleClass domain)
    {
        return new publishDtoNameSpace.ExampleClass{Test = domain.Test, SomeOtherTest = domain.SomeOtherTest};
    }
}