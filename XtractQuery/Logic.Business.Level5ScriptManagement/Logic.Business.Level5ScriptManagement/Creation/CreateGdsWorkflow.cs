using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.Script.Gds;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateGdsWorkflow(
    ILevel5ScriptParser scriptParser,
    IGdsCodeUnitConverter treeConverter,
    IGdsScriptWriter scriptWriter)
    : ICreateGdsWorkflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);
        GdsScriptFile script = treeConverter.CreateScriptFile(codeUnit);

        // Write script data
        scriptWriter.Write(script, output);
    }
}