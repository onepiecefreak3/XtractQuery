using Logic.Domain.Level5.Contract.Enums.Compression;

namespace Logic.Domain.Level5.Contract.DataClasses.Script;

public class CompressedScriptStringTable : ScriptStringTable
{
    public CompressionType? CompressionType { get; set; }
}