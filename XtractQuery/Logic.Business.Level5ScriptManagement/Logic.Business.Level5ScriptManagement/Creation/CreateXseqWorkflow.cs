using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script.Xseq;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateXseqWorkflow(
    ScriptManagementConfiguration config,
    ILevel5ScriptParser scriptParser,
    ILowLevelCodeUnitConverter lowLevelConverter,
    INamedGlobalSlotPass namedGlobalSlotPass,
    INamedLocalSlotPass namedLocalSlotPass,
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
        codeUnit = lowLevelConverter.Convert(codeUnit);
        codeUnit = namedGlobalSlotPass.Convert(codeUnit);
        codeUnit = namedLocalSlotPass.Convert(codeUnit);

        ScriptFile script = treeConverter.CreateScriptFile(codeUnit);
        script.Length = PointerLength.Int;

        // Write script data
        scriptWriter.Write(script, output, !config.WithoutCompression);
    }
}