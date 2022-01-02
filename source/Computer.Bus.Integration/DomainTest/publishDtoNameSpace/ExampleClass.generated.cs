using System.Collections.Generic;
using ProtoBuf;

namespace publishDtoNameSpace;
[ProtoContract]
public partial class ExampleClass
{
    [ProtoMember(1)]
    public string? Test { get; set; }

    [ProtoMember(2)]
    public IReadOnlyList<ulong> SomeOtherTest { get; init; }
}