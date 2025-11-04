using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gds;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractGdsWorkflow(
    IGdsScriptParser scriptParser,
    IGdsScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractGdsWorkflow
{
    public void Extract(Stream input, Stream output)
    {
        // Read script data
        GdsScriptFile script = scriptParser.Parse(input);

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script);
        whiteSpaceNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }
}