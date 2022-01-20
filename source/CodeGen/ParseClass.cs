using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
            .AddClassAttribute(protoClassAttribute)
            .MakePartial();
        var subscribeDto = root
            .AddUsing(protoBufUsing)
            .ChangeNameSpace(subscribeDtoNamespace)
            .AddClassAttribute(protoClassAttribute)
            .MakePartial();

        var assignDtoToDomain = new List<ExpressionSyntax>();
        var assignDomainToDto = new List<ExpressionSyntax>();
        
        var propertyCounter = 0;
        foreach (var property in properties)
        {
            var propertyName = property.Identifier.ValueText;
            propertyCounter++;
            // if (property.Type is GenericNameSyntax genericType)
            // {
            //     var x = genericType.TypeArgumentList.Arguments.First();
            // }
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
            .AddUsing("Computer.Bus.Domain.Contracts")
            .AddUsing("System")
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(publishDtoNamespace))
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new []
                    {
                        publishMapperClass
                    }))
            }));
        var subscribeDtoMapper = SyntaxFactory.CompilationUnit()
            .AddUsing("Computer.Bus.Domain.Contracts")
            .AddUsing("System")
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.IdentifierName(subscribeDtoNamespace))
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new []
                    {
                        subscribeMapperClass
                    }))
            }));

        var publishDomain = root
            .ChangeNameSpace(publishDomainNamespace)
            .MakePartial();
        var subscribeDomain = root
            .ChangeNameSpace(subscribeDomainNamespace)
            .MakePartial();

        return new ClassDef(
            publishDto.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())), 
            publishDtoMapper.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())), 
            publishDomain.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())), 
            subscribeDto.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())), 
            subscribeDtoMapper.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())), 
            subscribeDomain.WithTargetNamespace(ns =>ns.WithTargetClass(c => c.AddNullableDirective())),
            className,
            CreateMapperName(className));
    }

    private static ClassDeclarationSyntax CreateMapperClass(string className, string dtoNamespace,
        string domainNamespace, 
        IEnumerable<ExpressionSyntax> assignDtoToDomain,
        IEnumerable<ExpressionSyntax> assignDomainToDto)
    {
        var domainToDtoParams = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("domainType"))
                    .WithType(IdentifierName("Type")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("obj"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("dtoType"))
                    .WithType(IdentifierName("Type")),
            })
        );
        var dtoToDomainParams = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("dtoType"))
                    .WithType(IdentifierName("Type")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("obj"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("domainType"))
                    .WithType(IdentifierName("Type")),
            })
        );
        var dtoClassType = SyntaxFactory.ParseTypeName($"{dtoNamespace}.{className}");
        var domainClassType = SyntaxFactory.ParseTypeName($"{domainNamespace}.{className}");
        var mapperClass = SyntaxFactory.ClassDeclaration(CreateMapperName(className))
            .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword) }))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("IMapper")))))
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                SyntaxFactory.MethodDeclaration(
                        returnType: SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
                        identifier: SyntaxFactory.Identifier("DtoToDomain"))
                    .WithParameterList(dtoToDomainParams)
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block(
                        GetMapStatements(false, dtoClassType, domainClassType, assignDtoToDomain)
                        )),
                SyntaxFactory.MethodDeclaration(
                        returnType: SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
                        identifier: SyntaxFactory.Identifier("DomainToDto"))
                    .WithParameterList(domainToDtoParams)
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block(
                        GetMapStatements(true, dtoClassType, domainClassType, assignDomainToDto)
                    )),
                
                SyntaxFactory.MethodDeclaration(
                        returnType: domainClassType,
                        identifier: SyntaxFactory.Identifier("DtoToDomain"))
                    .WithParameterList( SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("dto"))
                                .WithType(dtoClassType),
                        })
                    ))
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block()),
                SyntaxFactory.MethodDeclaration(
                        returnType: dtoClassType,
                        identifier: SyntaxFactory.Identifier("DomainToDto"))
                    .WithParameterList( SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("domain"))
                                .WithType(domainClassType),
                        })
                    ))
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithBody(SyntaxFactory.Block())
            }));
        return mapperClass;
    }

    private static IEnumerable<StatementSyntax> GetMapStatements(
        bool isDomainToDto, 
        TypeSyntax dtoClassType, TypeSyntax domainClassType,
        IEnumerable<ExpressionSyntax> assignmentExpressions)
    {
        var fromType = isDomainToDto
            ? domainClassType
            : dtoClassType;
        var toType = isDomainToDto
            ? dtoClassType
            : domainClassType;
        var localVar = isDomainToDto
            ? Identifier("domain")
            : Identifier("dto");
        var localVarName = isDomainToDto
            ? IdentifierName("domain")
            : IdentifierName("dto");
        yield return IfStatement
        (
            BinaryExpression
            (
                SyntaxKind.LogicalOrExpression,
                BinaryExpression
                (
                    SyntaxKind.LogicalOrExpression,
                    PrefixUnaryExpression
                    (
                        SyntaxKind.LogicalNotExpression,
                        InvocationExpression
                            (
                                MemberAccessExpression
                                (
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("dtoType"),
                                    IdentifierName("IsAssignableFrom")
                                )
                            )
                            .WithArgumentList
                            (
                                ArgumentList
                                (
                                    SingletonSeparatedList<ArgumentSyntax>
                                    (
                                        Argument
                                        (
                                            TypeOfExpression
                                            (
                                                dtoClassType
                                            )
                                        )
                                    )
                                )
                            )
                    ),
                    PrefixUnaryExpression
                    (
                        SyntaxKind.LogicalNotExpression,
                        InvocationExpression
                            (
                                MemberAccessExpression
                                (
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("domainType"),
                                    IdentifierName("IsAssignableFrom")
                                )
                            )
                            .WithArgumentList
                            (
                                ArgumentList
                                (
                                    SingletonSeparatedList<ArgumentSyntax>
                                    (
                                        Argument
                                        (
                                            TypeOfExpression
                                            (
                                                domainClassType
                                            )
                                        )
                                    )
                                )
                            )
                    )
                ),
                BinaryExpression
                (
                    SyntaxKind.EqualsExpression,
                    IdentifierName("obj"),
                    LiteralExpression
                    (
                        SyntaxKind.NullLiteralExpression
                    )
                )
            ),
            Block
            (
                SingletonList<StatementSyntax>
                (
                    ReturnStatement
                    (
                        LiteralExpression
                        (
                            SyntaxKind.NullLiteralExpression
                        )
                    )
                )
            )
        );
        yield return LocalDeclarationStatement
        (
            VariableDeclaration
                (
                    IdentifierName
                    (
                        Identifier
                        (
                            TriviaList(),
                            SyntaxKind.VarKeyword,
                            "var",
                            "var",
                            TriviaList()
                        )
                    )
                )
                .WithVariables
                (
                    SingletonSeparatedList<VariableDeclaratorSyntax>
                    (
                        VariableDeclarator
                            (
                                localVar
                            )
                            .WithInitializer
                            (
                                EqualsValueClause
                                (
                                    BinaryExpression
                                    (
                                        SyntaxKind.AsExpression,
                                        IdentifierName("obj"),
                                        fromType
                                    )
                                )
                            )
                    )
                )
        );
        yield return IfStatement
        (
            BinaryExpression
            (
                SyntaxKind.EqualsExpression,
                localVarName,
                LiteralExpression
                (
                    SyntaxKind.NullLiteralExpression
                )
            ),
            Block
            (
                SingletonList<StatementSyntax>
                (
                    ReturnStatement
                    (
                        LiteralExpression
                        (
                            SyntaxKind.NullLiteralExpression
                        )
                    )
                )
            )
        );
        var convertMethodName = isDomainToDto ? IdentifierName("DomainToDto") : IdentifierName("DtoToDomain");
        yield return ReturnStatement
        (
            InvocationExpression
                (
                    convertMethodName
                )
                .WithArgumentList
                (
                    ArgumentList
                    (
                        SingletonSeparatedList<ArgumentSyntax>
                        (
                            Argument
                            (
                                localVarName
                            )
                        )
                    )
                )
        );
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
    public static CompilationUnitSyntax WithTargetNamespace(this CompilationUnitSyntax root,
        Func<BaseNamespaceDeclarationSyntax, BaseNamespaceDeclarationSyntax> changeNamespace)
    {
        var @namespace = root
            .FindTargetNamespace();
        var newNamespace = changeNamespace(@namespace);
        return root.ReplaceNode(
            @namespace,
            newNamespace);
    }
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

    public static CompilationUnitSyntax MakePartial(this CompilationUnitSyntax root)
    {
        var foundNamespace = root.FindTargetNamespace();
        var newNamespace = foundNamespace.MakePartial();
        return root.ReplaceNode(
            foundNamespace,
            newNamespace);
    }
}

public static class NamespaceSyntaxExtensions
{
    public static BaseNamespaceDeclarationSyntax WithTargetClass(this BaseNamespaceDeclarationSyntax root,
        Func<ClassDeclarationSyntax, ClassDeclarationSyntax> changeClass)
    {
        var @class = root
            .FindTargetClass();
        var newClass = changeClass(@class);
        return root.ReplaceNode(
            @class,
            newClass);
    }
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
    
    public static BaseNamespaceDeclarationSyntax MakePartial(this BaseNamespaceDeclarationSyntax namespaceDec)
    {
        var foundClass = namespaceDec.FindTargetClass();
        var newClass = foundClass.MakePartial();
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
    
    public static ClassDeclarationSyntax MakePartial(this ClassDeclarationSyntax classDec)
    {
        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
        if (classDec.Modifiers.Any(m => m.ValueText == partialToken.ValueText))
        {
            return classDec;
        }
        return classDec.AddModifiers(partialToken);
    }

    public static ClassDeclarationSyntax AddNullableDirective(this ClassDeclarationSyntax classDec)
    {
        return classDec.WithModifiers
        (
            TokenList
            (
                Token
                (
                    TriviaList
                    (
                        Trivia
                        (
                            NullableDirectiveTrivia
                            (
                                Token(SyntaxKind.EnableKeyword),
                                true
                            )
                        )
                    ),
                    SyntaxKind.PublicKeyword,
                    TriviaList()
                )
            )
        );
    }
}