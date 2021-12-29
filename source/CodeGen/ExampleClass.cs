//using ProtoBuf;

namespace CodeGen;

public class ExampleClass
{
    //[ProtoMember(1)]
    public string Test { get; set; }
    //[ProtoMember(2)]
    public IReadOnlyList<ulong> SomeOtherTest { get; init; }
}