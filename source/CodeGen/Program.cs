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

var pathToTargetClass = config["pathToTargetClass"];
if (string.IsNullOrWhiteSpace(pathToTargetClass))
{
    throw new ArgumentException("missing target path config");
}

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

if (string.IsNullOrWhiteSpace(config["outPutPath"]))
{
    throw new ArgumentException("Missing output path config");
}

var outPutDirectory = Directory.CreateDirectory(config["outPutPath"]);
if (outPutDirectory == null || !outPutDirectory.Exists)
{
    throw new ArgumentException("Something went wrong creating the output directory");
}

var tree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(pathToTargetClass)));

var root = (CompilationUnitSyntax)tree.GetRoot();
var p = new ParseClass();
var classDef = p.CreateClass(root,
    publishDtoNameSpace,
    publishDomainNameSpace,
    subscribeDtoNameSpace,
    subscribeDomainNameSpace);

var pubDtoDirectory =
    Directory.CreateDirectory(Path.Combine(outPutDirectory.FullName, SanitizeNamespaceToPath(publishDtoNameSpace)));
var pubDomainDirectory =
    Directory.CreateDirectory(Path.Combine(outPutDirectory.FullName, SanitizeNamespaceToPath(publishDomainNameSpace)));
var subDtoDirectory =
    Directory.CreateDirectory(Path.Combine(outPutDirectory.FullName, SanitizeNamespaceToPath(subscribeDtoNameSpace)));
var subDomainDirectory =
    Directory.CreateDirectory(Path.Combine(outPutDirectory.FullName, SanitizeNamespaceToPath(subscribeDomainNameSpace)));

await WriteClassFile(classDef.ClassName, classDef.PublishDto, pubDtoDirectory.FullName);
await WriteClassFile(classDef.MapperClassName, classDef.PublishDtoMapper, pubDtoDirectory.FullName);
await WriteClassFile(classDef.ClassName, classDef.PublishDomain, pubDomainDirectory.FullName);

await WriteClassFile(classDef.ClassName, classDef.SubscribeDto, subDtoDirectory.FullName);
await WriteClassFile(classDef.MapperClassName, classDef.SubscribeDtoMapper, subDtoDirectory.FullName);
await WriteClassFile(classDef.ClassName, classDef.SubscribeDomain, subDomainDirectory.FullName);


string SanitizeNamespaceToPath(string @namespace)
{
    return @namespace.Replace(".", "_");
}

async Task WriteClassFile(string className, CompilationUnitSyntax compilationUnitSyntax, string directoryPath)
{
    var fileName = $"{className}.generated.cs";
    var fullPath = Path.Combine(directoryPath, fileName);
    await using var publishDtoWriter = new StreamWriter(fullPath, false);
    compilationUnitSyntax.NormalizeWhitespace().WriteTo(publishDtoWriter);
}


internal record ClassDef(
    CompilationUnitSyntax PublishDto,
    CompilationUnitSyntax PublishDtoMapper,
    CompilationUnitSyntax PublishDomain,
    CompilationUnitSyntax SubscribeDto,
    CompilationUnitSyntax SubscribeDtoMapper,
    CompilationUnitSyntax SubscribeDomain,
    string ClassName,
    string MapperClassName);