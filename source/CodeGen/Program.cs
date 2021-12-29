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

var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(firstPath)));

var root = (CompilationUnitSyntax)tree.GetRoot();
var modelCollector = new ModelCollector();
modelCollector.Visit(root);

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

var classDef = modelCollector.CreateClass(publishDtoNameSpace,publishDomainNameSpace, subscribeDtoNameSpace, subscribeDomainNameSpace);
await using var publishDtoWriter = new StreamWriter(@"publishDto.generated.cs", false);
    classDef.PublishDto.NormalizeWhitespace().WriteTo(publishDtoWriter);
await using var subscribeDtoWriter = new StreamWriter(@"subscribeDto.generated.cs", false);
    classDef.SubscribeDto.NormalizeWhitespace().WriteTo(subscribeDtoWriter);


internal record ClassDef(
    CompilationUnitSyntax PublishDto,
    CompilationUnitSyntax PublishDtoMapper,
    CompilationUnitSyntax PublishDomain,
    CompilationUnitSyntax SubscribeDto,
    CompilationUnitSyntax SubscribeDtoMapper,
    CompilationUnitSyntax SubscribeDomain);