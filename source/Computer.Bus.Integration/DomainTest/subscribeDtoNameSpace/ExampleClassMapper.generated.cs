namespace subscribeDtoNameSpace;
public partial class ExampleClassMapper
{
    public subscribeDomainNameSpace.ExampleClass DtoToDomain(subscribeDtoNameSpace.ExampleClass dto)
    {
        return new subscribeDomainNameSpace.ExampleClass{Test = dto.Test, SomeOtherTest = dto.SomeOtherTest};
    }

    public subscribeDtoNameSpace.ExampleClass DomainToDto(subscribeDomainNameSpace.ExampleClass domain)
    {
        return new subscribeDtoNameSpace.ExampleClass{Test = domain.Test, SomeOtherTest = domain.SomeOtherTest};
    }
}