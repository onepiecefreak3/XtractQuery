namespace Logic.Domain.CodeAnalysis.DataClasses.Level5;

internal struct WhitespaceNormalizeContext
{
    public int Indent { get; set; }

    public bool ShouldIndent { get; set; }
    public bool ShouldLineBreak { get; set; }
    public bool IsFirstElement { get; set; }
}