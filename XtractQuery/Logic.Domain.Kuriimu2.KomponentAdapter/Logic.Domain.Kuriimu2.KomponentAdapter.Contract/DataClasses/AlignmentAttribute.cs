namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AlignmentAttribute : Attribute
{
    public int Alignment { get; }

    public AlignmentAttribute(int align)
    {
        Alignment = align;
    }
}