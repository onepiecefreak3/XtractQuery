namespace Logic.Business.Level5ScriptManagement.InternalContract.Decompression;

internal interface IDecompressXq32Workflow
{
    void Decompress(Stream input, Stream output);
}