namespace Logic.Business.Level5ScriptManagement.InternalContract.Decompression;

internal interface IDecompressXscrWorkflow
{
    void Decompress(Stream input, Stream output);
}