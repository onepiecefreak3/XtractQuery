namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
public class EndiannessAttribute : Attribute
{
    public ByteOrder ByteOrder = ByteOrder.LittleEndian;
}