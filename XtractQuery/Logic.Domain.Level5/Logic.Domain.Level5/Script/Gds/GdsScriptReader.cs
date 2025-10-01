using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Script.Gds;

class GdsScriptReader(IBinaryFactory binaryFactory) : IGdsScriptReader
{
    public GdsArgument[] Read(Stream input)
    {
        using IBinaryReaderX reader = binaryFactory.CreateReader(input, Encoding.GetEncoding("Shift-JIS"), true);

        int scriptSize = reader.ReadInt32();
        if (input.Length - 4 != scriptSize)
            throw new InvalidOperationException("Script size does not match actual size.");

        var result = new List<GdsArgument>();

        while (input.Position < input.Length)
        {
            result.Add(ReadArgument(reader));

            if (result[^1].type is 0xC)
                break;
        }

        return [.. result];
    }

    private GdsArgument ReadArgument(IBinaryReaderX reader)
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
                value = reader.ReadUInt32();
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