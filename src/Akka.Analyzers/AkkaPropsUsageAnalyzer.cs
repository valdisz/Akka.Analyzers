using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Akka.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AkkaPropsUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AkkaPropsUsageAnalyzer";

        private const string Title = "Props constructor was used to create new Props instance";
        private const string MessageFormat = "Props constructor is used only for serialization.";
        private const string Description = "Use factory methods of Props class to create new instances.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node.DeclaredNamespaceName().StartsWith("Akka")) return;

            var objCreation = (ObjectCreationExpressionSyntax) context.Node;
            var type = objCreation.Type;

            var typeSymbol = context.SemanticModel.GetSymbolInfo(type).Symbol;
            if (typeSymbol.ContainingNamespace.Name != "Akka") return;
            if (typeSymbol.Name != "Props") return;

            var diagnostic = Diagnostic.Create(Rule, objCreation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
