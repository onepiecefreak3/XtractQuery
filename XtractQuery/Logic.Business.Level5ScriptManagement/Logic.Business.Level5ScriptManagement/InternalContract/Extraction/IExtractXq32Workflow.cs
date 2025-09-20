namespace Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

internal interface IExtractXq32Workflow
{
    void Prepare();

    void Extract(Stream input, Stream output);
}