using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen;

public class ParseClass
{
    internal ClassDef CreateClass(
        CompilationUnitSyntax root,
        string publishDtoNamespace,
        string publishDomainNamespace,
        string subscribeDtoNamespace,
        string subscribeDomainNamespace)
    {
        var nameSpace = root.FindTargetNamespace();
        var classDef = nameSpace.FindTargetClass();
        var properties = classDef.Members
            .OfType<PropertyDeclarationSyntax>()
            //.Where(p => p is public and has get/set)
            .ToArray();

        const string protoBufUsing = "ProtoBuf";
        const string protoClassAttribute = "ProtoContract";
        var publishDto = root
            .AddUsing(protoBufUsing)
            .ChangeNameSpace(publishDtoNamespace)
            .AddClassAttribute(protoClassAttribute);
        var subscribeDto = root
            .AddUsing(protoBufUsing)
            .ChangeNameSpace(subscribeDtoNamespace)
            .AddClassAttribute(protoClassAttribute);

        var assignDtoToDomain = new List<ExpressionSyntax>();
        var assignDomainToDto = new List<ExpressionSyntax>();
        
        var propertyCounter = 0;
        foreach (var property in properties)
        {
            var propertyName = property.Identifier.ValueText;
            propertyCounter++;
            publishDto = AddProtoPropAttribute(publishDto, propertyName, propertyCounter);
            subscribeDto = AddProtoPropAttribute(subscribeDto, propertyName, propertyCounter).MakePropertyNullable(propertyName);
            
            assignDtoToDomain.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(propertyName),
                SyntaxFactory.IdentifierName($"dto.{propertyName}")
                ));
            assignDomainToDto.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(propertyName),
                SyntaxFactory.IdentifierName($"domain.{propertyName}")
            ));
        }

        var className = classDef.Identifier.ValueText;
        var publishMapperClass = CreateMapperClass(className, publishDtoNamespace, publishDomainNamespace, assignDtoToDomain, assignDomainToDto);
        var subscribeMapperClass = CreateMapperClass(className, subscribeDtoNamespace, subscribeDomainNamespace, assignDtoToDomain, assignDomainToDto);

        var publishDtoMapper = SyntaxFactory.CompilationUnit()
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(publishDtoNamespace))
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new []
                    {
                        publishMapperClass
                    }))
            }));
        var subscribeDtoMapper = SyntaxFactory.CompilationUnit()
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(subscribeDtoNamespace))
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new []
                    {
                        subscribeMapperClass
                    }))
            }));

        var publishDomain = root.ChangeNameSpace(publishDomainNamespace);
        var subscribeDomain = root.ChangeNameSpace(subscribeDomainNamespace);

        return new ClassDef(
            publishDto, 
            publishDtoMapper, 
            publishDomain, 
            subscribeDto, 
            subscribeDtoMapper, 
            subscribeDomain,
            className,
            CreateMapperName(className));
    }

    private static ClassDeclarationSyntax CreateMapperClass(string className, string dtoNamespace,
        string domainNamespace, 
        IEnumerable<ExpressionSyntax> assignDtoToDomain,
        IEnumerable<ExpressionSyntax> assignDomainToDto)
    {
        var domainToDtoParams = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("domain"))
                    .WithType(SyntaxFactory.ParseTypeName($"{domainNamespace}.{className}"))
            })
        );
        var dtoToDomainParams = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("dto"))
                    .WithType(SyntaxFactory.ParseTypeName($"{dtoNamespace}.{className}"))
            })
        );
        var mapperClass = SyntaxFactory.ClassDeclaration(CreateMapperName(className))
            .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.MethodDeclaration(
                        returnType: SyntaxFactory.ParseTypeName($"{domainNamespace}.{className}"),
                        identifier: SyntaxFactory.Identifier("DtoToDomain"))
                    .WithParameterList(dtoToDomainParams)
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName($"{domainNamespace}.{className}"))
                            .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,SyntaxFactory.SeparatedList<ExpressionSyntax>(assignDtoToDomain))))
                        )),
                SyntaxFactory.MethodDeclaration(
                        returnType: SyntaxFactory.ParseTypeName($"{dtoNamespace}.{className}"),
                        identifier: SyntaxFactory.Identifier("DomainToDto"))
                    .WithParameterList(domainToDtoParams)
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName($"{domainNamespace}.{className}"))
                            .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,SyntaxFactory.SeparatedList<ExpressionSyntax>(assignDomainToDto))))
                    ))
            }));
        return mapperClass;
    }

    private static string CreateMapperName(string className)
    {
        return $"{className}Mapper";
    }

    private CompilationUnitSyntax AddProtoPropAttribute(CompilationUnitSyntax root,
        string propertyName,
        int propertyCounter)
    {
        var foundNamespace = root.FindTargetNamespace();
        var newNamespace = AddProtoPropAttribute(foundNamespace, propertyName, propertyCounter);
        return root.ReplaceNode(
            foundNamespace,
            newNamespace);
    }

    private BaseNamespaceDeclarationSyntax AddProtoPropAttribute(BaseNamespaceDeclarationSyntax nameSpace,
        string propertyName,
        int propertyCounter)
    {
        var foundClass = nameSpace.FindTargetClass();
        var newClass = AddProtoPropAttribute(foundClass, propertyName, propertyCounter);
        return nameSpace.ReplaceNode(
            foundClass,
            newClass);
    }

    private ClassDeclarationSyntax AddProtoPropAttribute(ClassDeclarationSyntax classDec,
        string propertyName,
        int propertyCounter)
    {
        var foundProperty = classDec.FindPropertyByName(propertyName);

        var newProperty = AddProtoPropAttribute(foundProperty, propertyCounter);
        return classDec.ReplaceNode(
            foundProperty,
            newProperty);
    }

    private static PropertyDeclarationSyntax AddProtoPropAttribute(PropertyDeclarationSyntax propertyDec,
        int propertyCounter)
    {
        var argEx = SyntaxFactory.ParseExpression($"{propertyCounter}");
        var arg = SyntaxFactory.AttributeArgument(argEx);
        var argumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new[] { arg }));

        var attributes = propertyDec.AttributeLists.Add(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ProtoMember"))
                    .WithArgumentList(argumentList)
            )));
        return propertyDec.WithAttributeLists(attributes);
    }
}

public static class CompilationUnitSyntaxExtensions
{
    public static CompilationUnitSyntax AddUsing(this CompilationUnitSyntax root, string identifier)
    {
        var name = SyntaxFactory.IdentifierName(identifier);
        return root.AddUsings(SyntaxFactory.UsingDirective(name));
    }
    
    public static CompilationUnitSyntax ChangeNameSpace(this CompilationUnitSyntax root, string nameSpace)
    {
        var foundNameSpace = root.FindTargetNamespace();
        var newNamespace = foundNameSpace.WithName(SyntaxFactory.IdentifierName(nameSpace));
        return root.ReplaceNode(
            foundNameSpace,
            newNamespace);
    }
    
    public static BaseNamespaceDeclarationSyntax FindTargetNamespace(this CompilationUnitSyntax root)
    {
        return root.Members
            .OfType<BaseNamespaceDeclarationSyntax>()
            .First();
    }
    
    public static CompilationUnitSyntax AddClassAttribute(this CompilationUnitSyntax root,
        string attributeName)
    {
        var foundNamespace = root.FindTargetNamespace();
        var newNamespace = foundNamespace.AddClassAttribute(attributeName);
        return root.ReplaceNode(
            foundNamespace,
            newNamespace);
    }

    public static CompilationUnitSyntax MakePropertyNullable(this CompilationUnitSyntax root,
        string propertyName)
    {
        var foundNamespace = root.FindTargetNamespace();
        var newNamespace = foundNamespace.MakePropertyNullable(propertyName);
        return root.ReplaceNode(
            foundNamespace,
            newNamespace);
    }
}

public static class NamespaceSyntaxExtensions
{
    public static BaseNamespaceDeclarationSyntax AddClassAttribute(this BaseNamespaceDeclarationSyntax namespaceDec,
        string attributeName)
    {
        var foundClass = namespaceDec.FindTargetClass();
        var newClass = foundClass.AddClassAttribute(attributeName);
        return namespaceDec.ReplaceNode(
            foundClass,
            newClass);
    }
    
    public static ClassDeclarationSyntax FindTargetClass(this BaseNamespaceDeclarationSyntax nameSpace)
    {
        return nameSpace.Members
            .OfType<ClassDeclarationSyntax>()
            .First();
    }

    public static BaseNamespaceDeclarationSyntax MakePropertyNullable(this BaseNamespaceDeclarationSyntax namespaceDec,
        string propertyName)
    {
        var foundClass = namespaceDec.FindTargetClass();
        var newClass = foundClass.MakePropertyNullable(propertyName);
        return namespaceDec.ReplaceNode(
            foundClass,
            newClass);
    }
}

public static class ClassSyntaxExtensions
{
    public static ClassDeclarationSyntax AddClassAttribute(this ClassDeclarationSyntax classDec,
        string attributeName)
    {
        var attributes = classDec.AttributeLists.Add(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName))
            )));
        return classDec.WithAttributeLists(attributes);
    }

    public static ClassDeclarationSyntax MakePropertyNullable(this ClassDeclarationSyntax classDec,
        string propertyName)
    {
        var foundProperty = classDec.FindPropertyByName(propertyName);
        if (foundProperty.Type is NullableTypeSyntax)
        {
            return classDec;
        }
        var newProperty = foundProperty.WithType(SyntaxFactory.NullableType(foundProperty.Type));
        return classDec.ReplaceNode(
            foundProperty,
            newProperty);
    }
    
    public static PropertyDeclarationSyntax FindPropertyByName(this ClassDeclarationSyntax classDec, string propertyName)
    {
        return classDec.Members
            .OfType<PropertyDeclarationSyntax>()
            .First(p => p.Identifier.ValueText == propertyName);
    }
}