namespace Akka.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class SyntaxExtensions
    {
        public static string DeclaredNamespaceName(this SyntaxNode node)
        {
            var ns = node.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            var name = ns.Name as IdentifierNameSyntax;

            return name?.Identifier.ValueText ?? "";
        }
    }
}