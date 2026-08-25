namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Resolves Level5-style positional <c>$temp</c> dests, skipping slots still named in the source.
/// </summary>
internal sealed class TempSlotFrame(HashSet<int> reservedSourceTemps)
{
    public int Resolve(int dest)
    {
        if (dest < 1)
            return dest;

        while (reservedSourceTemps.Contains(dest))
            dest++;

        return dest;
    }
}
