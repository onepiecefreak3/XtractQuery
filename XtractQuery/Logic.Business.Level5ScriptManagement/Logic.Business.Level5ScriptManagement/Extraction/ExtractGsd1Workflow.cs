using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractGsd1Workflow(
    IGsd1ScriptParser scriptParser,
    IGsd1ScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractGsd1Workflow
{
    public void Extract(Stream input, Stream output)
    {
        // Read script data
        Gsd1ScriptFile script = scriptParser.Parse(input);

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script);
        whiteSpaceNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }
}