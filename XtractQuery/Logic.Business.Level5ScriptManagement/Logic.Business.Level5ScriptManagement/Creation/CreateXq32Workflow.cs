using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.Level5.Contract.Script.Xq32;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateXq32Workflow(
    ScriptManagementConfiguration config,
    ILevel5ScriptParser scriptParser,
    ILevel5CodeUnitConverter treeConverter,
    IXq32ScriptWriter scriptWriter)
    : ICreateXq32Workflow
{
    public void Create(Stream input, Stream output)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);

        ScriptFile script = treeConverter.CreateScriptFile(codeUnit);
        script.Length = DeterminePointerLength(config.Length);

        // Write script data
        scriptWriter.Write(script, output, !config.WithoutCompression);
    }

    private PointerLength DeterminePointerLength(string type)
    {
        switch (type)
        {
            case "int":
                return PointerLength.Int;

            case "long":
                return PointerLength.Long;

            default:
                throw new InvalidOperationException($"Unsupported pointer length {type}");
        }
    }
}