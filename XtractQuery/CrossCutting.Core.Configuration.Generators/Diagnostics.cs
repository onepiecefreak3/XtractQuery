using Microsoft.CodeAnalysis;

namespace CrossCutting.Core.Configuration.Generators;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor VirtualPropertyNotAllowed = new(
        id: "COCOCFG001",
        title: "Configuration property must not be virtual",
        messageFormat: "Property '{0}' on configuration type '{1}' must not be virtual",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConfigurationKeyCategoryInvalid = new(
        id: "COCOCFG002",
        title: "ConfigurationKey category must be CommandLine",
        messageFormat: "Property '{0}' uses ConfigurationKey with category '{1}'; only 'CommandLine' is allowed",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConfigurationKeyKeysEmpty = new(
        id: "COCOCFG003",
        title: "ConfigurationKey keys must not be empty",
        messageFormat: "Property '{0}' has a ConfigurationKey attribute without keys",
        category: "Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
