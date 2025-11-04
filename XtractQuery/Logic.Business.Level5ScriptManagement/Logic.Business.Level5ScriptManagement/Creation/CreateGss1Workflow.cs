using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.Level5.Contract.Script.Gss1;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateGss1Workflow(
    ILevel5ScriptParser scriptParser,
    IGss1CodeUnitConverter treeConverter,
    IGss1ScriptWriter scriptWriter)
    : ICreateGss1Workflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);
        Gss1ScriptFile script = treeConverter.CreateScriptFile(codeUnit);

        // Write script data
        scriptWriter.Write(script, output);
    }
}