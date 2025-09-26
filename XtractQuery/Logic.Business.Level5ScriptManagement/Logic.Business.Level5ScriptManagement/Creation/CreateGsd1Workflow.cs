using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateGsd1Workflow(
    ILevel5ScriptParser scriptParser,
    IGsd1CodeUnitConverter treeConverter,
    IGsd1ScriptWriter scriptWriter)
    : ICreateGsd1Workflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);
        Gsd1ScriptFile script = treeConverter.CreateScriptFile(codeUnit);

        // Write script data
        scriptWriter.Write(script, output);
    }
}