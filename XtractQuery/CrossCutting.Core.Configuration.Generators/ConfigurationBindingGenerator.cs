using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CrossCutting.Core.Configuration.Generators;

[Generator]
public sealed class ConfigurationBindingGenerator : IIncrementalGenerator
{
    private const string CategoryAttributeMetadataName =
        "CrossCutting.Core.Contract.Configuration.DataClasses.ConfigurationCategoryAttribute";

    private const string KeyAttributeMetadataName =
        "CrossCutting.Core.Contract.Configuration.DataClasses.ConfigurationKeyAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ConfigTypeModel?> configTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => CreateModel(ctx))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(configTypes, static (spc, model) =>
        {
            if (model is null)
                return;

            foreach (Diagnostic diagnostic in model.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            if (model.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            spc.AddSource(
                $"{model.TypeName}.ConfigurationFactory.g.cs",
                SourceText.From(GenerateSource(model), Encoding.UTF8));
        });
    }

    private static ConfigTypeModel? CreateModel(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        if (typeSymbol.TypeKind != TypeKind.Class || typeSymbol.IsAbstract)
            return null;

        AttributeData? categoryAttribute = typeSymbol.GetAttributes()
            .FirstOrDefault(a => IsAttribute(a, CategoryAttributeMetadataName));

        if (categoryAttribute is null)
            return null;

        string? categoryName = GetConstructorStringArgument(categoryAttribute, 0);
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        var diagnostics = new List<Diagnostic>();
        var properties = new List<ConfigPropertyModel>();

        foreach (IPropertySymbol property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (property.SetMethod is null || property.GetMethod is null)
                continue;

            Location location = property.Locations.FirstOrDefault() ?? Location.None;

            if (HasVirtualModifier(property))
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.VirtualPropertyNotAllowed,
                    location,
                    property.Name,
                    typeSymbol.Name));
            }

            AttributeData? keyAttribute = property.GetAttributes()
                .FirstOrDefault(a => IsAttribute(a, KeyAttributeMetadataName));

            string bindCategory = categoryName!;
            ImmutableArray<string> keys = ImmutableArray.Create(property.Name);

            if (keyAttribute is not null)
            {
                string? keyCategory = GetConstructorStringArgument(keyAttribute, 0);

                if (!string.Equals(keyCategory, "CommandLine", StringComparison.Ordinal))
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.ConfigurationKeyCategoryInvalid,
                        location,
                        property.Name,
                        keyCategory ?? "<null>"));
                }
                else
                {
                    bindCategory = "CommandLine";
                }

                keys = ExtractKeys(keyAttribute);
                if (keys.IsDefaultOrEmpty || keys.All(string.IsNullOrWhiteSpace))
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.ConfigurationKeyKeysEmpty,
                        location,
                        property.Name));
                    keys = ImmutableArray.Create(property.Name);
                }
            }

            bool isRequired = property.IsRequired;

            properties.Add(new ConfigPropertyModel(
                property.Name,
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                bindCategory,
                keys,
                isRequired));
        }

        string? ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new ConfigTypeModel(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ns,
            categoryName!,
            properties.ToImmutableArray(),
            diagnostics.ToImmutableArray());
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        INamedTypeSymbol? attributeClass = attribute.AttributeClass;
        if (attributeClass is null)
            return false;

        string display = attributeClass.ToDisplayString();
        if (display == metadataName)
            return true;

        return attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{metadataName}";
    }

    private static string? GetConstructorStringArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index)
            return null;

        return attribute.ConstructorArguments[index].Value as string;
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

    private static ImmutableArray<string> ExtractKeys(AttributeData keyAttribute)
    {
        if (keyAttribute.ConstructorArguments.Length < 2)
            return ImmutableArray<string>.Empty;

        TypedConstant keysArg = keyAttribute.ConstructorArguments[1];
        if (keysArg.Kind == TypedConstantKind.Array)
        {
            return keysArg.Values
                .Select(v => v.Value as string)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToImmutableArray();
        }

        if (keysArg.Value is string singleKey && !string.IsNullOrWhiteSpace(singleKey))
            return ImmutableArray.Create(singleKey);

        return ImmutableArray<string>.Empty;
    }

    private static string GenerateSource(ConfigTypeModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using CrossCutting.Core.Contract.Configuration;");
        sb.AppendLine("using CrossCutting.Core.Configuration.ConfigObjects;");
        sb.AppendLine();

        string factoryNamespace = model.Namespace is null
            ? "GeneratedConfiguration"
            : $"{model.Namespace}.GeneratedConfiguration";

        sb.AppendLine($"namespace {factoryNamespace};");
        sb.AppendLine();
        sb.AppendLine($"internal static class {model.TypeName}ConfigurationFactory");
        sb.AppendLine("{");
        sb.AppendLine("    [ModuleInitializer]");
        sb.AppendLine("    internal static void Register()");
        sb.AppendLine("    {");
        sb.AppendLine($"        ConfigObjectFactoryRegistry.Register<{model.FullyQualifiedTypeName}>(Create);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private static {model.FullyQualifiedTypeName} Create(IConfigurator configurator)");
        sb.AppendLine("    {");

        ImmutableArray<ConfigPropertyModel> required = model.Properties.Where(p => p.IsRequired).ToImmutableArray();
        ImmutableArray<ConfigPropertyModel> optional = model.Properties.Where(p => !p.IsRequired).ToImmutableArray();

        if (required.Length == 0)
        {
            sb.AppendLine($"        var config = new {model.FullyQualifiedTypeName}();");
        }
        else
        {
            sb.AppendLine($"        var config = new {model.FullyQualifiedTypeName}");
            sb.AppendLine("        {");
            for (int i = 0; i < required.Length; i++)
            {
                ConfigPropertyModel property = required[i];
                string keysLiteral = ToKeysLiteral(property.Keys);
                string comma = i == required.Length - 1 ? string.Empty : ",";
                sb.AppendLine(
                    $"            {property.Name} = ConfigObjectBinder.GetRequired<{property.TypeDisplayName}>(configurator, \"{Escape(property.Category)}\", {keysLiteral}){comma}");
            }

            sb.AppendLine("        };");
        }

        foreach (ConfigPropertyModel property in optional)
        {
            string keysLiteral = ToKeysLiteral(property.Keys);
            sb.AppendLine(
                $"        ConfigObjectBinder.AssignIfPresent<{property.TypeDisplayName}>(configurator, \"{Escape(property.Category)}\", value => config.{property.Name} = value, {keysLiteral});");
        }

        sb.AppendLine("        return config;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string ToKeysLiteral(ImmutableArray<string> keys)
        => string.Join(", ", keys.Select(k => $"\"{Escape(k)}\""));

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

internal sealed record ConfigTypeModel(
    string TypeName,
    string FullyQualifiedTypeName,
    string? Namespace,
    string CategoryName,
    ImmutableArray<ConfigPropertyModel> Properties,
    ImmutableArray<Diagnostic> Diagnostics);

internal sealed record ConfigPropertyModel(
    string Name,
    string TypeDisplayName,
    string Category,
    ImmutableArray<string> Keys,
    bool IsRequired);
