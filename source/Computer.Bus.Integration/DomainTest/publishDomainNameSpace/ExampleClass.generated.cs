using System.Collections.Generic;

namespace publishDomainNameSpace;
public partial class ExampleClass
{
    public string? Test { get; set; }

    public IReadOnlyList<ulong> SomeOtherTest { get; init; }
}