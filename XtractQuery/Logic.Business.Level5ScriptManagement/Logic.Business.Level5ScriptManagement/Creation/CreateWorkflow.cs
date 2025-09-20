using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using System.Diagnostics.CodeAnalysis;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateWorkflow(
    ScriptManagementConfiguration config,
    ICreateXq32Workflow createXq32Workflow,
    ICreateXseqWorkflow createXseqWorkflow)
    : ICreateWorkflow
{
    public void Create()
    {
        if (!TryDetermineScriptType(config.QueryType, out ScriptType? type))
        {
            Console.WriteLine("Unknown script type.");
            return;
        }

        bool isDirectory = Directory.Exists(config.InputPath);
        if (isDirectory)
            CreateDirectory(config.InputPath, type.Value);
        else
            CreateFile(config.InputPath, type.Value);
    }

    private void CreateDirectory(string dirPath, ScriptType type)
    {
        string[] files = Directory.GetFiles(dirPath, "*.txt", SearchOption.AllDirectories);

        foreach (string file in files)
            CreateFile(file, type);
    }

    private void CreateFile(string filePath, ScriptType type)
    {
        Console.Write($"Compile {filePath}... ");

        bool wasSuccessful = TryCreateFile(filePath, type, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");
    }

    private bool TryCreateFile(string filePath, ScriptType type, [NotNullWhen(false)] out Exception? error)
    {
        error = null;

        using Stream inputStream = File.OpenRead(filePath);

        try
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    using (Stream outputStream = File.Create(filePath + ".xq"))
                        createXq32Workflow.Create(inputStream, outputStream);
                    break;

                case ScriptType.Xseq:
                    using (Stream outputStream = File.Create(filePath + ".xq"))
                        createXseqWorkflow.Create(inputStream, outputStream);
                    break;
            }
        }
        catch (Exception e)
        {
            error = e;
            return false;
        }

        return true;
    }

    private static bool TryDetermineScriptType(string type, [NotNullWhen(true)] out ScriptType? scriptType)
    {
        scriptType = null;

        switch (type)
        {
            case "xq32":
                scriptType = ScriptType.Xq32;
                break;

            case "xseq":
                scriptType = ScriptType.Xseq;
                break;

            default:
                return false;
        }

        return true;
    }

    private static Exception GetInnermostException(Exception e)
    {
        while (e.InnerException != null)
            e = e.InnerException;

        return e;
    }
}