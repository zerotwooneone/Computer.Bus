using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal class ModelCollector : CSharpSyntaxWalker
{
    private ClassDeclarationSyntax? _publishClassDec;
    private ClassDeclarationSyntax? _subscribeClassDec;
    private CompilationUnitSyntax? _root;
    private int PropertyCounter = 0;
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (_publishClassDec == null)
        {
            throw new NotImplementedException("no namespace was found");
        }

        if (_publishClassDec.Identifier.ValueText != ((ClassDeclarationSyntax?)node.Parent)?.Identifier.ValueText)
        {
            throw new NotImplementedException("class names do not match. nesting is not supported yet.");
        }

        PropertyCounter++;
        var argEx = SyntaxFactory.ParseExpression($"{PropertyCounter}");
        var arg = SyntaxFactory.AttributeArgument(argEx);
        var argumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new []{arg}));
        
        var attributes = node.AttributeLists.Add(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ProtoMember"))
                    .WithArgumentList(argumentList)
            )).NormalizeWhitespace());
        var withAttributes = node.WithAttributeLists(attributes);
        var newNode = withAttributes;
        
        //we have to find the node to replace in the class def because it may contain a clone
        var cNode = FindPropertyByName(node.Identifier.ValueText, _publishClassDec);
        _publishClassDec = _publishClassDec.ReplaceNode(
            cNode,
            newNode);
        
        var nullableNode = withAttributes.WithType(SyntaxFactory.NullableType(node.Type));
        
        var nNode = FindPropertyByName(node.Identifier.ValueText, _publishClassDec);
        _subscribeClassDec = _publishClassDec.ReplaceNode(
            nNode,
            nullableNode);
        
        base.VisitPropertyDeclaration(node);
    }

    private MemberDeclarationSyntax FindPropertyByName(string name, ClassDeclarationSyntax c)
    {
        return c.Members.First(m =>
            m is PropertyDeclarationSyntax p && p.Identifier.ValueText == name);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        
        var x = node.Parent;
        throw new NotImplementedException("Cannot handle namespaces yet");
        base.VisitNamespaceDeclaration(node);
    }

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
        _root = node;
        base.VisitCompilationUnit(node);
    }
    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        
        var x = node.Parent;
        if (node.Members.First() is not ClassDeclarationSyntax c)
        {
            throw new NotImplementedException("Cannot handle non-root classes yet");
        }

        _publishClassDec = c;
        _subscribeClassDec = c;
        base.VisitFileScopedNamespaceDeclaration(node);
    }
 
    public ClassDef CreateClass(
        string publishDtoNamespace,
        string publishDomainNamespace,
        string subscribeDtoNamespace,
        string subscribeDomainNamespace)
    {
        if (_root == null)
        {
            throw new NotImplementedException("root was null");
        }
        var publishDomain = ChangeNamespace(_root, publishDomainNamespace);
        var publishDto = CreateProtobufClass(_publishClassDec, publishDtoNamespace);
        //var publishDtoMapper = CreateDtoMapper()
        var subscribeDomain = ChangeNamespace(_root, subscribeDomainNamespace);
        var subscribeDto = CreateProtobufClass(_subscribeClassDec, subscribeDtoNamespace);
        
        return new ClassDef(
            publishDto, 
            SyntaxFactory.CompilationUnit(),
            publishDomain,
            subscribeDto,
            SyntaxFactory.CompilationUnit(),
            subscribeDomain);
    }

    private CompilationUnitSyntax ChangeNamespace(CompilationUnitSyntax input, string newNamespace)
    {
        var namespaces = input.Members
            .Where(m => m is FileScopedNamespaceDeclarationSyntax)
            .ToArray();
        if (!namespaces.Any())
        {
            throw new NotImplementedException("must have a file scoped namespace for now");
        }
        CompilationUnitSyntax result = SyntaxFactory.CompilationUnit();
        foreach (var member in namespaces)
        {
            var ns = (FileScopedNamespaceDeclarationSyntax)member;
            var newNs = ns.WithName(SyntaxFactory.IdentifierName(newNamespace));
            result = input.ReplaceNode(
                member,
                newNs);
        }
        
        return result;
    }

    private static CompilationUnitSyntax CreateProtobufClass(ClassDeclarationSyntax? compUnit, string nameSpace)
    {
        var member = AddClassAttribute(compUnit, "ProtoContract");
        string protoBufUsing = "ProtoBuf";
        var name = SyntaxFactory.IdentifierName(protoBufUsing);
        var ns = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nameSpace)).AddMembers(member);
        var cus = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(name))
            .AddMembers(ns);
        return cus;
    }

    private static MemberDeclarationSyntax AddClassAttribute(ClassDeclarationSyntax? compUnit,
        string attributeName)
    {
        var attributes = compUnit.AttributeLists.Add(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName))
            )).NormalizeWhitespace());
        var x = compUnit.WithAttributeLists(attributes);
        var member = (MemberDeclarationSyntax)x;
        return member;
    }
}