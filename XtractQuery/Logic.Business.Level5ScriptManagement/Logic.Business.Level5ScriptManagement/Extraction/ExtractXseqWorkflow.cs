using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.Xseq;
using System.Text.RegularExpressions;

namespace Logic.Business.Level5ScriptManagement.Extraction;

partial class ExtractXseqWorkflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IXseqFunctionCache functionCache,
    IXseqScriptDecompressor scriptDecompressor,
    IXseqScriptReader scriptReader,
    IXseqScriptFileConverter scriptConverter,
    ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer,
    ILevel5ScriptComposer scriptComposer)
    : IExtractXseqWorkflow
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
        if (_isPopulated)
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
            string relativePath = Path.GetRelativePath(referenceDir, scriptFile);
            string? relativeDir = Path.GetDirectoryName(relativePath);
            string relativeFileName = Path.GetFileNameWithoutExtension(scriptFile);
            string relativeName = string.IsNullOrEmpty(relativeDir) ? relativeFileName : Path.Combine(relativeDir, relativeFileName);
            relativeName = SanitizeNamespace(relativeName);

            using Stream scriptStream = File.OpenRead(scriptFile);
            ScriptType type = typeReader.Peek(scriptStream);

            if (type is not ScriptType.Xseq)
                continue;

            CompressedScriptTable functionTable = scriptDecompressor.DecompressFunctions(scriptStream);
            CompressedScriptStringTable stringTable = scriptDecompressor.DecompressStrings(scriptStream);

            IList<ScriptFunction> functions = scriptReader.ReadFunctions(functionTable, stringTable);

            foreach (ScriptFunction function in functions)
            {
                if (!functionCache.TryAdd(relativeName, function.Name))
                    Console.WriteLine($"Could not cache function {function.Name} from script {relativePath}.");
            }
        }

        _isPopulated = true;
    }

    [GeneratedRegex(@"^\d+$", RegexOptions.Compiled)]
    private static partial Regex IsNumber();

    private static string SanitizeNamespace(string nameSpace)
    {
        nameSpace = nameSpace.Replace('.', '_');

        string[] pathParts = nameSpace.Split(Path.DirectorySeparatorChar);
        nameSpace = Path.Combine(pathParts.Select(p => IsNumber().IsMatch(p) ? $"_{p}" : p).ToArray());

        return nameSpace;
    }
}