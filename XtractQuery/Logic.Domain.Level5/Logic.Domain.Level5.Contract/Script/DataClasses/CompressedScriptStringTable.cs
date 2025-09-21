using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.DataClasses;

public class CompressedScriptStringTable : ScriptStringTable
{
    public CompressionType? CompressionType { get; set; }
}