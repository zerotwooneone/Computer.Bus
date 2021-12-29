﻿using Microsoft.CodeAnalysis;
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
    public Dictionary<string, string?> Models { get; } = new();
    private List<ClassDeclarationSyntax> classes = new();
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var classnode = node.Parent as ClassDeclarationSyntax;
        if (!Models.ContainsKey(classnode.Identifier.ValueText))
        {
            Models.Add(classnode.Identifier.ValueText, null);
            classes.Add(classnode);
        }
    }
    
    public NamespaceDeclarationSyntax CreateClass()
    {
        var ns = NamespaceDeclaration(ParseName("CodeGen")).AddMembers(classes.ToArray());
        return ns;
    }
}