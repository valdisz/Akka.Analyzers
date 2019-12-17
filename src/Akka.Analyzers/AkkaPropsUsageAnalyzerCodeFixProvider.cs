using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Akka.Analyzers
{
    using System.Reflection.Metadata;
    using Document = Microsoft.CodeAnalysis.Document;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AkkaPropsUsageAnalyzerCodeFixProvider)), Shared]
    public class AkkaPropsUsageAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Use Props factory method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AkkaPropsUsageAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var expr = root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<ObjectCreationExpressionSyntax>()
                .First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedSolution: c => UseFactoryMethodAsync(context.Document, expr, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Solution> UseFactoryMethodAsync(Document document, ObjectCreationExpressionSyntax expr, CancellationToken cancellationToken)
        {
            var semantic = await document.GetSemanticModelAsync(cancellationToken);

            var args = expr.ArgumentList.Arguments;
            if (args.Count == 0) return null;

            var firstArg = semantic.GetSymbolInfo(args[0], cancellationToken);

            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}
