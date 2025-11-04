using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateXscrWorkflow(
    ILevel5ScriptParser scriptParser,
    IXscrCodeUnitConverter treeConverter,
    IXscrScriptWriter scriptWriter)
    : ICreateXscrWorkflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);
        XscrScriptFile script = treeConverter.CreateScriptFile(codeUnit);

        // Write script data
        scriptWriter.Write(script, output);
    }
}