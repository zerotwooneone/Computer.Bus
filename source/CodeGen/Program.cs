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

var ns = modelCollector.CreateClass();
await using var streamWriter = new StreamWriter(@"generated.cs", false);
    ns.NormalizeWhitespace().WriteTo(streamWriter);
 

        
class ModelCollector : CSharpSyntaxWalker
{
    private ClassDeclarationSyntax? ClassDec;
    private int PropertyCounter = 0;
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (ClassDec == null)
        {
            throw new NotImplementedException("no namespace was found");
        }

        if (ClassDec.Identifier.ValueText != ((ClassDeclarationSyntax?)node.Parent)?.Identifier.ValueText)
        {
            throw new NotImplementedException("class names do not match. nesting is not supported yet.");
        }

        PropertyCounter++;
        var argEx = ParseExpression($"{PropertyCounter}");
        var arg = AttributeArgument(argEx);
        var argumentList = AttributeArgumentList(SeparatedList(new []{arg}));
        
        var attributes = node.AttributeLists.Add(
            AttributeList(SingletonSeparatedList<AttributeSyntax>(
                Attribute(IdentifierName("ProtoMember"))
                .WithArgumentList(argumentList)
            )).NormalizeWhitespace());
        var newNode = node.WithAttributeLists(attributes);
        
        //we have to find the node to replace in the class def because it may contain a clone
        var cNode = ClassDec.Members.First(m =>
            m is PropertyDeclarationSyntax p && p.Identifier.ValueText == node.Identifier.ValueText);
        ClassDec = ClassDec.ReplaceNode(
            cNode,
            newNode);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        
        var x = node.Parent;
        throw new NotImplementedException("Cannot handle namespaces yet");
        base.VisitNamespaceDeclaration(node);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        
        var x = node.Parent;
        if (node.Members.First() is not ClassDeclarationSyntax c)
        {
            throw new NotImplementedException("Cannot handle non-root classes yet");
        }

        ClassDec = c;
        base.VisitFileScopedNamespaceDeclaration(node);
    }
 
    public CompilationUnitSyntax CreateClass(string nameSpace = "CodeGen")
    {
        var attributes = ClassDec.AttributeLists.Add(
            SyntaxFactory.AttributeList(SingletonSeparatedList(
                Attribute(IdentifierName("ProtoContract"))
            )).NormalizeWhitespace());
        var x = ClassDec.WithAttributeLists(attributes);
        var member = (MemberDeclarationSyntax)x;
        var name = IdentifierName("ProtoBuf");
        var ns = NamespaceDeclaration(ParseName(nameSpace)).AddMembers(member);
        var cus = CompilationUnit().AddUsings(UsingDirective(name)).AddMembers(ns);
        return cus;
    }
}