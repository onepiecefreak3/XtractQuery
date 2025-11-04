using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.Gss1;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractGss1Workflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IGss1ScriptReader scriptReader,
    IGss1ScriptParser scriptParser,
    IGss1ScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractGss1Workflow
{
    private bool _isPopulated;

    public void Prepare()
    {
        PopulateStringHashCache();
    }

    public void Extract(Stream input, Stream output)
    {
        // Read script data
        Gss1ScriptFile script = scriptParser.Parse(input);

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script);
        whiteSpaceNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }

    private void PopulateStringHashCache()
    {
        if (!_isPopulated)
            return;

        string referenceDir = config.ReferenceScriptPath;
        if (string.IsNullOrEmpty(referenceDir))
            return;

        string? dirName = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrEmpty(dirName))
            return;

        referenceDir = Path.Combine(dirName, referenceDir);
        if (!Directory.Exists(referenceDir))
            return;

        foreach (string scriptFile in Directory.GetFiles(referenceDir, "*.cq", SearchOption.AllDirectories))
        {
            using Stream scriptStream = File.OpenRead(scriptFile);
            ScriptType type = typeReader.Peek(scriptStream);

            if (type is not ScriptType.Gss1)
                continue;

            Gss1ScriptContainer container = scriptReader.Read(scriptStream);
            scriptParser.ParseFunctions(container.Functions, container.Strings);
        }

        _isPopulated = true;
    }
}