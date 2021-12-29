using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Configuration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

Console.WriteLine("Hello, World!");

var configBuilder = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddJsonFile(@"Config\default.json");
var config = configBuilder.Build();

var firstPath = config["firstPath"];

var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(firstPath)));

var root = (CompilationUnitSyntax)tree.GetRoot();
var modelCollector = new ModelCollector();
modelCollector.Visit(root);

var x = 0;

// var members = schema?.Types.Select(t => CreateClass(t.TypeName)).ToArray() 
//               ?? Array.Empty<MemberDeclarationSyntax>();
//  
// var ns = NamespaceDeclaration(ParseName("CodeGen")).AddMembers(members);

var ns = modelCollector.CreateClass();
await using var streamWriter = new StreamWriter(@"generated.cs", false);
    ns.NormalizeWhitespace().WriteTo(streamWriter);
 

        
class ModelCollector : CSharpSyntaxWalker
{
    public Dictionary<string, List<string>> Models { get; } = new Dictionary<string, List<string>>();
    private List<ClassDeclarationSyntax> classes = new();
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var classnode = node.Parent as ClassDeclarationSyntax;
        if (!Models.ContainsKey(classnode.Identifier.ValueText))
        {
            Models.Add(classnode.Identifier.ValueText, new List<string>());
            classes.Add(classnode);
        }

        Models[classnode.Identifier.ValueText].Add(node.Identifier.ValueText);
    }
    
    public NamespaceDeclarationSyntax CreateClass()
    {
        //var classes = new List<ClassDeclarationSyntax>();
        //foreach in classes
        // var c = ClassDeclaration(Identifier(name))
        //     .AddModifiers(Token(SyntaxKind.PublicKeyword));
        var ns = NamespaceDeclaration(ParseName("CodeGen")).AddMembers(classes.ToArray());
        return ns;
    }
}