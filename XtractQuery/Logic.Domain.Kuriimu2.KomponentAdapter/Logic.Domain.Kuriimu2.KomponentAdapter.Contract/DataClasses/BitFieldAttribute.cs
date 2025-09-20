namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

[AttributeUsage(AttributeTargets.Field)]
public class BitFieldAttribute : Attribute
{
    public int BitLength { get; }

    public BitFieldAttribute(int bitLength)
    {
        BitLength = bitLength;
    }
}