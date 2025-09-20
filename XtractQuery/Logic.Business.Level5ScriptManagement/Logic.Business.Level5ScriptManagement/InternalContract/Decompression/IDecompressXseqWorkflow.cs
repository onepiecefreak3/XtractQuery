namespace Logic.Business.Level5ScriptManagement.InternalContract.Decompression;

internal interface IDecompressXseqWorkflow
{
    void Decompress(Stream input, Stream output);
}