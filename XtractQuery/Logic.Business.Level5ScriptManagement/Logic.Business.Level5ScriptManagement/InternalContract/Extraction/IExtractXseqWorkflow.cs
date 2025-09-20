namespace Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

internal interface IExtractXseqWorkflow
{
    void Prepare();

    void Extract(Stream input, Stream output);
}