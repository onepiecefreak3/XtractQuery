using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CrossCutting.Core.Configuration.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConfigurationAnalyzer : DiagnosticAnalyzer
{
    private const string CategoryAttributeMetadataName =
        "CrossCutting.Core.Contract.Configuration.DataClasses.ConfigurationCategoryAttribute";

    private const string KeyAttributeMetadataName =
        "CrossCutting.Core.Contract.Configuration.DataClasses.ConfigurationKeyAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Diagnostics.VirtualPropertyNotAllowed, Diagnostics.ConfigurationKeyCategoryInvalid, Diagnostics.ConfigurationKeyKeysEmpty];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
            return;

        bool hasCategory = typeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == CategoryAttributeMetadataName);

        if (!hasCategory)
            return;

        foreach (IPropertySymbol property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
                continue;

            Location location = property.Locations.FirstOrDefault() ?? Location.None;

            if (HasVirtualModifier(property))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.VirtualPropertyNotAllowed,
                    location,
                    property.Name,
                    typeSymbol.Name));
            }

            AttributeData? keyAttribute = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == KeyAttributeMetadataName);

            if (keyAttribute is null)
                continue;

            string? keyCategory = keyAttribute.ConstructorArguments.Length > 0
                ? keyAttribute.ConstructorArguments[0].Value as string
                : null;

            if (!string.Equals(keyCategory, "CommandLine", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ConfigurationKeyCategoryInvalid,
                    location,
                    property.Name,
                    keyCategory ?? "<null>"));
            }

            bool hasKeys = false;
            if (keyAttribute.ConstructorArguments.Length >= 2)
            {
                TypedConstant keysArg = keyAttribute.ConstructorArguments[1];
                if (keysArg.Kind == TypedConstantKind.Array)
                    hasKeys = keysArg.Values.Any(v => v.Value is string s && !string.IsNullOrWhiteSpace(s));
                else if (keysArg.Value is string single && !string.IsNullOrWhiteSpace(single))
                    hasKeys = true;
            }

            if (!hasKeys)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ConfigurationKeyKeysEmpty,
                    location,
                    property.Name));
            }
        }
    }

    private static bool HasVirtualModifier(IPropertySymbol property)
    {
        foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax propertySyntax)
                return propertySyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword));
        }

        return false;
    }
}
