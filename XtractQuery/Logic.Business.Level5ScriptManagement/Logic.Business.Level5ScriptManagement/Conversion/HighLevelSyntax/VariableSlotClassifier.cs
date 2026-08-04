using System.Text.RegularExpressions;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Classifies variable tokens as explicit typed slots (<c>$local0</c>, <c>$local0_name</c>)
/// or free-form named locals (<c>$counter</c>).
/// </summary>
internal static partial class VariableSlotClassifier
{
    public const int LocalSlotCount = 1000;
    public const int GlobalSlotCount = 1000;

    public static bool TryGetTypedSlot(string text, out string type, out int slot)
    {
        type = string.Empty;
        slot = -1;

        Match match = TypedVariablePattern().Match(text);
        if (!match.Success)
            return false;

        type = match.Groups[1].Value;
        slot = int.Parse(match.Groups[2].Value);
        return true;
    }

    public static bool IsNamedVariable(string text)
    {
        return text.StartsWith('$') && !TryGetTypedSlot(text, out _, out _);
    }

    public static bool IsNamedLocal(string text) => IsNamedVariable(text);

    public static bool TryGetExplicitLocalSlot(string text, out int slot)
    {
        slot = -1;
        if (!TryGetTypedSlot(text, out string type, out int typedSlot))
            return false;

        if (type != "local")
            return false;

        slot = typedSlot;
        return true;
    }

    public static bool TryGetExplicitGlobalSlot(string text, out int slot)
    {
        slot = -1;
        if (!TryGetTypedSlot(text, out string type, out int typedSlot))
            return false;

        if (type != "global")
            return false;

        slot = typedSlot;
        return true;
    }

    [GeneratedRegex(@"^\$(ctx|temp|local|param|global)([0-9]+)(_.*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex TypedVariablePattern();
}
