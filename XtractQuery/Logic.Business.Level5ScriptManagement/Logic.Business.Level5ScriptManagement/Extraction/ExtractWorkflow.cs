using System.Diagnostics.CodeAnalysis;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractWorkflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IExtractXq32Workflow extractXq32Workflow,
    IExtractXseqWorkflow extractXseqWorkflow)
    : IExtractWorkflow
{
    public void Extract()
    {
        bool isDirectory = Directory.Exists(config.InputPath);
        if (isDirectory)
            ExtractDirectory(config.InputPath);
        else
            ExtractFile(config.InputPath);
    }

    private void ExtractDirectory(string dirPath)
    {
        string[] files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

        foreach (string file in files)
            ExtractFile(file);
    }

    private void ExtractFile(string filePath)
    {
        Console.Write($"Extract {filePath}... ");

        using Stream inputStream = File.OpenRead(filePath);
        using Stream outputStream = File.Create(filePath + ".txt");

        if (!TryPeekType(inputStream, out ScriptType? type))
        {
            Console.WriteLine("Unsupported script type.");
            return;
        }

        bool wasSuccessful = TryExtractFile(inputStream, outputStream, type.Value, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");
    }

    private bool TryExtractFile(Stream input, Stream output, ScriptType type, [NotNullWhen(false)] out Exception? error)
    {
        error = null;

        try
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    extractXq32Workflow.Prepare();
                    extractXq32Workflow.Extract(input, output);
                    break;

                case ScriptType.Xseq:
                    extractXseqWorkflow.Prepare();
                    extractXseqWorkflow.Extract(input, output);
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

    private bool TryPeekType(Stream input, [NotNullWhen(true)] out ScriptType? type)
    {
        type = null;

        try
        {
            type = typeReader.Peek(input);
        }
        catch (Exception)
        {
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