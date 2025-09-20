using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.DataClasses;

public class ScriptTable
{
    public int EntryCount { get; set; }
    public CompressionType? CompressionType { get; set; }
    public Stream Stream { get; set; }
}