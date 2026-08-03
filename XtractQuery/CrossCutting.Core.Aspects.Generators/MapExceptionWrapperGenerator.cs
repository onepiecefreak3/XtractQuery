using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CrossCutting.Core.Aspects.Generators;

[Generator]
public sealed class MapExceptionWrapperGenerator : IIncrementalGenerator
{
    private const string MapExceptionMetadataName =
        "CrossCutting.Core.Contract.Aspects.MapExceptionAttribute";

    private const string ExceptionMessageMetadataName =
        "CrossCutting.Core.Contract.Aspects.ExceptionMessageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<InterfaceModel?> interfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => CreateModel(ctx))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(interfaces, static (spc, model) =>
        {
            if (model is null)
                return;

            spc.AddSource(
                $"{model.SafeFileName}.ExceptionMapping.g.cs",
                SourceText.From(GenerateSource(model), Encoding.UTF8));
        });
    }

    private static InterfaceModel? CreateModel(GeneratorSyntaxContext context)
    {
        if (context.Node is not InterfaceDeclarationSyntax interfaceDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        if (typeSymbol.TypeKind != TypeKind.Interface)
            return null;

        AttributeData? mapException = typeSymbol.GetAttributes()
            .FirstOrDefault(a => IsAttribute(a, MapExceptionMetadataName));

        if (mapException is null)
            return null;

        INamedTypeSymbol? targetException = mapException.ConstructorArguments.Length > 0
            ? mapException.ConstructorArguments[0].Value as INamedTypeSymbol
            : null;

        if (targetException is null)
            return null;

        string? message = mapException.ConstructorArguments.Length > 1
            ? mapException.ConstructorArguments[1].Value as string
            : null;

        var methods = new List<MethodModel>();
        var properties = new List<PropertyModel>();

        CollectMembers(typeSymbol, methods, properties);

        foreach (INamedTypeSymbol baseInterface in typeSymbol.AllInterfaces)
            CollectMembers(baseInterface, methods, properties);

        return new InterfaceModel(
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.Name,
            targetException.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            message,
            methods.ToImmutableArray(),
            properties.ToImmutableArray(),
            SanitizeFileName(typeSymbol.ToDisplayString()));
    }

    private static void CollectMembers(
        INamedTypeSymbol typeSymbol,
        List<MethodModel> methods,
        List<PropertyModel> properties)
    {
        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            switch (member)
            {
                case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary && !method.IsStatic:
                    MethodModel candidate = CreateMethodModel(method);
                    if (methods.Any(m => IsSameSignature(m, candidate)))
                        break;

                    methods.Add(candidate);
                    break;

                case IPropertySymbol property when !property.IsStatic:
                    if (properties.Any(p => p.Name == property.Name))
                        break;

                    properties.Add(CreatePropertyModel(property));
                    break;
            }
        }
    }

    private static PropertyModel CreatePropertyModel(IPropertySymbol property)
    {
        return new PropertyModel(
            property.Name,
            property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            property.GetMethod is not null,
            property.SetMethod is not null);
    }

    private static MethodModel CreateMethodModel(IMethodSymbol method)
    {
        AttributeData? exceptionMessage = method.GetAttributes()
            .FirstOrDefault(a => IsAttribute(a, ExceptionMessageMetadataName));

        string? methodMessage = exceptionMessage?.ConstructorArguments.Length > 0
            ? exceptionMessage.ConstructorArguments[0].Value as string
            : null;

        ImmutableArray<string> forwardAttributes = method.GetAttributes()
            .Where(a => IsAttribute(a, "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute")
                        || IsAttribute(a, "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute"))
            .Select(FormatAttribute)
            .Where(s => s is not null)
            .Cast<string>()
            .ToImmutableArray();

        var typeParameterNames = method.TypeParameters
            .Select(tp => tp.Name)
            .ToImmutableArray();

        var typeParameterDeclarations = method.TypeParameters
            .Select(FormatTypeParameterDeclaration)
            .ToImmutableArray();

        var constraints = method.TypeParameters
            .Select(tp => FormatConstraints(tp))
            .Where(c => c is not null)
            .Cast<string>()
            .ToImmutableArray();

        var parameters = method.Parameters
            .Select(p => new ParameterModel(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                p.RefKind,
                p.HasExplicitDefaultValue,
                p.HasExplicitDefaultValue ? FormatDefault(p) : null,
                p.IsParams))
            .ToImmutableArray();

        return new MethodModel(
            method.Name,
            method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.ReturnsVoid,
            method.Arity,
            typeParameterNames,
            typeParameterDeclarations,
            constraints,
            parameters,
            methodMessage,
            forwardAttributes);
    }

    private static string FormatTypeParameterDeclaration(ITypeParameterSymbol typeParameter)
    {
        AttributeData? dam = typeParameter.GetAttributes()
            .FirstOrDefault(a => IsAttribute(a, "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"));

        if (dam is null || dam.ConstructorArguments.Length == 0)
            return typeParameter.Name;

        TypedConstant arg = dam.ConstructorArguments[0];
        string membersExpr;
        if (arg.Kind == TypedConstantKind.Enum && arg.Type is not null)
        {
            string enumType = arg.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string? name = arg.Type.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, arg.Value))
                ?.Name;
            membersExpr = name is not null
                ? $"{enumType}.{name}"
                : $"({enumType}){Convert.ToInt64(arg.Value)}";
        }
        else
        {
            membersExpr = Convert.ToInt64(arg.Value).ToString();
        }

        return $"[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers({membersExpr})] {typeParameter.Name}";
    }

    private static string? FormatAttribute(AttributeData attribute)
    {
        string typeName = attribute.AttributeClass!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (attribute.ConstructorArguments.Length == 0)
            return $"[{typeName}]";

        var args = new List<string>();
        foreach (TypedConstant arg in attribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string s)
                args.Add(SymbolDisplay.FormatLiteral(s, true));
            else if (arg.Kind == TypedConstantKind.Enum)
                args.Add($"({arg.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){Convert.ToInt64(arg.Value)}");
            else if (arg.Value is null)
                args.Add("null");
            else
                args.Add(arg.Value.ToString() ?? "null");
        }

        return $"[{typeName}({string.Join(", ", args)})]";
    }

    private static bool IsSameSignature(MethodModel left, MethodModel right)
    {
        if (left.Name != right.Name || left.Arity != right.Arity || left.Parameters.Length != right.Parameters.Length)
            return false;

        for (int i = 0; i < left.Parameters.Length; i++)
        {
            if (left.Parameters[i].TypeName != right.Parameters[i].TypeName
                || left.Parameters[i].RefKind != right.Parameters[i].RefKind)
                return false;
        }

        return true;
    }

    private static string? FormatConstraints(ITypeParameterSymbol typeParameter)
    {
        var parts = new List<string>();

        if (typeParameter.HasReferenceTypeConstraint)
            parts.Add("class");
        if (typeParameter.HasValueTypeConstraint)
            parts.Add("struct");
        if (typeParameter.HasNotNullConstraint)
            parts.Add("notnull");
        if (typeParameter.HasUnmanagedTypeConstraint)
            parts.Add("unmanaged");

        foreach (ITypeSymbol constraint in typeParameter.ConstraintTypes)
            parts.Add(constraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        if (typeParameter.HasConstructorConstraint)
            parts.Add("new()");

        if (parts.Count == 0)
            return null;

        return $"where {typeParameter.Name} : {string.Join(", ", parts)}";
    }

    private static string FormatDefault(IParameterSymbol parameter)
    {
        if (parameter.ExplicitDefaultValue is null)
            return "default!";

        return parameter.ExplicitDefaultValue switch
        {
            string s => SymbolDisplay.FormatLiteral(s, true),
            bool b => b ? "true" : "false",
            char c => SymbolDisplay.FormatLiteral(c, true),
            _ => parameter.ExplicitDefaultValue.ToString() ?? "default!"
        };
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return attribute.AttributeClass?.ToDisplayString() == metadataName;
    }

    private static string SanitizeFileName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString();
    }

    private static string GenerateSource(InterfaceModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using CrossCutting.Core.Contract.Aspects;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace};");
            sb.AppendLine();
        }

        string wrapperName = $"{model.TypeName}ExceptionMapping";
        string messageLiteral = model.Message is null
            ? "null"
            : SymbolDisplay.FormatLiteral(model.Message, true);

        sb.AppendLine($"internal sealed class {wrapperName} : {model.FullyQualifiedName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {model.FullyQualifiedName} _inner;");
        sb.AppendLine();
        sb.AppendLine($"    public {wrapperName}({model.FullyQualifiedName} inner)");
        sb.AppendLine("    {");
        sb.AppendLine("        _inner = inner ?? throw new ArgumentNullException(nameof(inner));");
        sb.AppendLine("    }");

        foreach (PropertyModel property in model.Properties)
        {
            sb.AppendLine();
            AppendProperty(sb, property, model.TargetExceptionFullyQualifiedName, messageLiteral);
        }

        foreach (MethodModel method in model.Methods)
        {
            sb.AppendLine();
            AppendMethod(sb, method, model.TargetExceptionFullyQualifiedName, messageLiteral);
        }

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("internal static class " + wrapperName + "Registrar");
        sb.AppendLine("{");
        sb.AppendLine("    [ModuleInitializer]");
        sb.AppendLine("    internal static void Register()");
        sb.AppendLine("    {");
        sb.AppendLine($"        ExceptionMappingRegistry.Register<{model.FullyQualifiedName}>(");
        sb.AppendLine($"            inner => new {wrapperName}(inner));");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendProperty(
        StringBuilder sb,
        PropertyModel property,
        string targetException,
        string typeMessageLiteral)
    {
        sb.AppendLine($"    public {property.TypeName} {property.Name}");
        sb.AppendLine("    {");

        if (property.HasGetter)
        {
            sb.AppendLine("        get");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine($"                return _inner.{property.Name};");
            sb.AppendLine("            }");
            sb.AppendLine($"            catch (Exception e) when (e is not {targetException})");
            sb.AppendLine("            {");
            sb.AppendLine($"                throw new {targetException}({typeMessageLiteral}, e);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }

        if (property.HasSetter)
        {
            sb.AppendLine("        set");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine($"                _inner.{property.Name} = value;");
            sb.AppendLine("            }");
            sb.AppendLine($"            catch (Exception e) when (e is not {targetException})");
            sb.AppendLine("            {");
            sb.AppendLine($"                throw new {targetException}({typeMessageLiteral}, e);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
    }

    private static void AppendMethod(
        StringBuilder sb,
        MethodModel method,
        string targetException,
        string typeMessageLiteral)
    {
        string declarationTypeParams = method.Arity > 0
            ? "<" + string.Join(", ", method.TypeParameterDeclarations) + ">"
            : string.Empty;
        string callTypeParams = method.Arity > 0
            ? "<" + string.Join(", ", method.TypeParameterNames) + ">"
            : string.Empty;

        var parameterList = new List<string>();
        var argumentList = new List<string>();
        var formatArgs = new List<string>();

        foreach (ParameterModel parameter in method.Parameters)
        {
            string refPrefix = parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => string.Empty
            };

            string paramsPrefix = parameter.IsParams ? "params " : string.Empty;

            string defaultSuffix = parameter.HasDefault
                ? $" = {parameter.DefaultValue}"
                : string.Empty;

            parameterList.Add($"{paramsPrefix}{refPrefix}{parameter.TypeName} {parameter.Name}{defaultSuffix}");
            argumentList.Add($"{refPrefix}{parameter.Name}");
            formatArgs.Add(parameter.Name);
        }

        foreach (string attribute in method.ForwardAttributes)
            sb.AppendLine($"    {attribute}");

        string returnType = method.ReturnsVoid ? "void" : method.ReturnTypeName;
        sb.AppendLine($"    public {returnType} {method.Name}{declarationTypeParams}({string.Join(", ", parameterList)})");

        foreach (string constraint in method.Constraints)
            sb.AppendLine($"        {constraint}");

        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");

        string call = $"_inner.{method.Name}{callTypeParams}({string.Join(", ", argumentList)})";
        if (method.ReturnsVoid)
            sb.AppendLine($"            {call};");
        else
            sb.AppendLine($"            return {call};");

        sb.AppendLine("        }");
        sb.AppendLine($"        catch (Exception e) when (e is not {targetException})");
        sb.AppendLine("        {");

        if (method.MethodMessage is not null)
        {
            string methodMessageLiteral = SymbolDisplay.FormatLiteral(method.MethodMessage, true);
            if (formatArgs.Count > 0)
            {
                sb.AppendLine($"            string message = string.Format({methodMessageLiteral}, {string.Join(", ", formatArgs)});");
            }
            else
            {
                sb.AppendLine($"            string message = {methodMessageLiteral};");
            }

            sb.AppendLine($"            throw new {targetException}(message, e);");
        }
        else
        {
            sb.AppendLine($"            throw new {targetException}({typeMessageLiteral}, e);");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private sealed record InterfaceModel(
        string Namespace,
        string FullyQualifiedName,
        string TypeName,
        string TargetExceptionFullyQualifiedName,
        string? Message,
        ImmutableArray<MethodModel> Methods,
        ImmutableArray<PropertyModel> Properties,
        string SafeFileName);

    private sealed record MethodModel(
        string Name,
        string ReturnTypeName,
        bool ReturnsVoid,
        int Arity,
        ImmutableArray<string> TypeParameterNames,
        ImmutableArray<string> TypeParameterDeclarations,
        ImmutableArray<string> Constraints,
        ImmutableArray<ParameterModel> Parameters,
        string? MethodMessage,
        ImmutableArray<string> ForwardAttributes);

    private sealed record PropertyModel(
        string Name,
        string TypeName,
        bool HasGetter,
        bool HasSetter);

    private sealed record ParameterModel(
        string Name,
        string TypeName,
        RefKind RefKind,
        bool HasDefault,
        string? DefaultValue,
        bool IsParams);
}
