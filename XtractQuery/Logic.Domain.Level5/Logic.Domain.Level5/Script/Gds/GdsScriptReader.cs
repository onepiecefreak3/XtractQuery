using System.Text;
using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gds;

namespace Logic.Domain.Level5.Script.Gds;

class GdsScriptReader : IGdsScriptReader
{
    private static readonly Encoding SjisEncoding = Encoding.GetEncoding("Shift-JIS");

    public GdsArgument[] Read(Stream input)
    {
        using var reader = new BinaryReaderX(input, SjisEncoding, true);

        int scriptSize = reader.ReadInt32();
        if (input.Length - 4 != scriptSize)
            throw new InvalidOperationException("Script size does not match actual size.");

        var result = new List<GdsArgument>();

        while (input.Position - 4 < scriptSize)
            result.Add(ReadArgument(reader));

        return [.. result];
    }

    private GdsArgument ReadArgument(BinaryReaderX reader)
    {
        int offset = (int)reader.BaseStream.Position - 4;
        short type = reader.ReadInt16();

        object? value;
        switch (type)
        {
            case 0:
                value = reader.ReadInt16();
                break;

            case 1:
            case 6:
            case 7:
                value = reader.ReadInt32();
                break;

            case 2:
                value = reader.ReadSingle();
                break;

            case 3:
                short stringSize = reader.ReadInt16();
                value = reader.ReadString(stringSize).TrimEnd('\0');
                break;

            case 8:
            case 9:
            case 11:
            case 12:
                value = null;
                break;

            default:
                throw new InvalidOperationException($"Unknown argument type {type} at position {reader.BaseStream.Position - 2}.");
        }

        return new GdsArgument
        {
            offset = offset,
            type = type,
            value = value
        };
    }
}