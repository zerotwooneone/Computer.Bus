using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

Console.WriteLine("Hello, World!");

var schema = new Schema
    { Types = new List<SchemaTypes> { new(typeName: "TypeName1", properties: new[] { "Prop1" }) } };

var members = schema?.Types.Select(t => CreateClass(t.TypeName)).ToArray() 
              ?? Array.Empty<MemberDeclarationSyntax>();
 
var ns = NamespaceDeclaration(ParseName("CodeGen")).AddMembers(members);
 
await using var streamWriter = new StreamWriter(@"generated.cs", false);
    ns.NormalizeWhitespace().WriteTo(streamWriter);
 
static ClassDeclarationSyntax CreateClass(string name) =>
    ClassDeclaration(Identifier(name))
        .AddModifiers(Token(SyntaxKind.PublicKeyword));
        
public class Schema
{
    public IReadOnlyCollection<SchemaTypes> Types { get; init; } = Array.Empty<SchemaTypes>();
}
 
public class SchemaTypes
{
    public SchemaTypes()
    {
    }

    public SchemaTypes(string typeName, IReadOnlyCollection<string> properties)
    {
        TypeName = typeName;
        Properties = properties;
    }

    public string TypeName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Properties { get; init; } = Array.Empty<string>();
}