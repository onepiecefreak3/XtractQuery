using System.Text;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses;

public class LengthInfo
{
    public int Length { get; }
    public Encoding Encoding { get; }

    public LengthInfo(int length, Encoding encoding)
    {
        Length = length;
        Encoding = encoding;
    }
}