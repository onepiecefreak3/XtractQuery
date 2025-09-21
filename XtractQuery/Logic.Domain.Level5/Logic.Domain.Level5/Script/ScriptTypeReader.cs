using System.Buffers.Binary;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Script;

internal class ScriptTypeReader : IScriptTypeReader
{
    public ScriptType Read(Stream stream)
    {
        if (stream.Length < 4)
            throw new InvalidOperationException("Stream needs to be at least 4 bytes long.");

        var buffer = new byte[4];
        int _ = stream.Read(buffer);

        if (buffer[0] == 'X' && buffer[1] == 'Q' && buffer[2] == '3' && buffer[3] == '2')
            return ScriptType.Xq32;

        if (buffer[0] == 'X' && buffer[1] == 'S' && buffer[2] == 'E' && buffer[3] == 'Q')
            return ScriptType.Xseq;

        if (buffer[0] == 'G' && buffer[1] == 'S' && buffer[2] == 'S' && buffer[3] == '1')
            return ScriptType.Gss1;

        throw new InvalidOperationException($"Unknown script type 0x{BinaryPrimitives.ReadUInt32BigEndian(buffer):X8}");
    }

    public ScriptType Peek(Stream stream)
    {
        var bkPos = stream.Position;
        var type = Read(stream);

        stream.Position = bkPos;
        return type;
    }
}