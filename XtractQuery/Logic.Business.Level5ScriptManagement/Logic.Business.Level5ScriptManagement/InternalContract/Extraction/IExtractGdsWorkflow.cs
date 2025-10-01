namespace Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

internal interface IExtractGdsWorkflow
{
    void Extract(Stream input, Stream output);
}