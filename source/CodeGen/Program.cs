using CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");

var configBuilder = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddJsonFile(@"Config\default.json");
var config = configBuilder.Build();

var firstPath = config["firstPath"];

if (string.IsNullOrWhiteSpace(config["publishDtoNameSpace"]) ||
    string.IsNullOrWhiteSpace(config["publishDomainNameSpace"]) ||
    string.IsNullOrWhiteSpace(config["subscribeDtoNameSpace"]) ||
    string.IsNullOrWhiteSpace(config["subscribeDomainNameSpace"]))
{
    throw new ArgumentException("missing required namespace config");
}

var publishDtoNameSpace = config["publishDtoNameSpace"];
var publishDomainNameSpace = config["publishDomainNameSpace"];
var subscribeDtoNameSpace = config["subscribeDtoNameSpace"];
var subscribeDomainNameSpace = config["subscribeDomainNameSpace"];

var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(firstPath)));

var root = (CompilationUnitSyntax)tree.GetRoot();
var p = new ParseClass();
var classDef = p.CreateClass(root,
    publishDtoNameSpace,
    publishDomainNameSpace,
    subscribeDtoNameSpace,
    subscribeDomainNameSpace);


await WriteClassFile("publishDto", classDef.PublishDto);
await WriteClassFile("publishDomain", classDef.PublishDomain);
await WriteClassFile("publishMapper", classDef.PublishDtoMapper);
await WriteClassFile("subscribeDto", classDef.SubscribeDto);
await WriteClassFile("subscribeDomain", classDef.SubscribeDomain);
await WriteClassFile("subscribeMapper", classDef.SubscribeDtoMapper);

async Task WriteClassFile(string className, CompilationUnitSyntax compilationUnitSyntax)
{
    await using var publishDtoWriter = new StreamWriter($"{className}.generated.cs", false);
    compilationUnitSyntax.NormalizeWhitespace().WriteTo(publishDtoWriter);
}


internal record ClassDef(
    CompilationUnitSyntax PublishDto,
    CompilationUnitSyntax PublishDtoMapper,
    CompilationUnitSyntax PublishDomain,
    CompilationUnitSyntax SubscribeDto,
    CompilationUnitSyntax SubscribeDtoMapper,
    CompilationUnitSyntax SubscribeDomain);