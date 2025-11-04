using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateXseqWorkflow(
    ScriptManagementConfiguration config,
    ILevel5ScriptParser scriptParser,
    IXseqCodeUnitConverter treeConverter,
    IXseqScriptWriter scriptWriter)
    : ICreateXseqWorkflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);

        ScriptFile script = treeConverter.CreateScriptFile(codeUnit);
        script.Length = PointerLength.Int;

        // Write script data
        scriptWriter.Write(script, output, !config.WithoutCompression);
    }
}