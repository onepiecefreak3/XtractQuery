using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractXq32Workflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IXq32ScriptDecompressor scriptDecompressor,
    IXq32ScriptReader scriptReader,
    IXq32ScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractXq32Workflow
{
    private bool _isPopulated;

    public void Prepare()
    {
        PopulateStringHashCache();
    }

    public void Extract(Stream input, Stream output)
    {
        // Read script data
        ScriptFile script = scriptReader.Read(input);

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

        foreach (string scriptFile in Directory.GetFiles(referenceDir, "*.xq", SearchOption.AllDirectories))
        {
            using Stream scriptStream = File.OpenRead(scriptFile);
            ScriptType type = typeReader.Peek(scriptStream);

            if (type is not ScriptType.Xq32)
                continue;

            CompressedScriptTable functionTable = scriptDecompressor.DecompressFunctions(scriptStream);
            CompressedScriptStringTable stringTable = scriptDecompressor.DecompressStrings(scriptStream);

            scriptReader.ReadFunctions(functionTable, stringTable);
        }

        _isPopulated = true;
    }
}