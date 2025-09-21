namespace Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

internal interface IExtractGss1Workflow
{
    void Prepare();

    void Extract(Stream input, Stream output);
}