using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractXscrWorkflow(
    IXscrScriptParser scriptParser,
    IXscrScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractXscrWorkflow
{
    public void Extract(Stream input, Stream output)
    {
        // Read script data
        XscrScriptFile script = scriptParser.Parse(input);

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script);
        whiteSpaceNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }
}