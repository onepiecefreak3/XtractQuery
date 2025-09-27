using Logic.Business.Level5ScriptManagement.InternalContract.Decompression;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using System.Diagnostics.CodeAnalysis;
using Logic.Domain.Level5.Contract.Script;

namespace Logic.Business.Level5ScriptManagement.Decompression;

class DecompressWorkflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IDecompressXq32Workflow decompressXq32Workflow,
    IDecompressXseqWorkflow decompressXseqWorkflow,
    IDecompressXscrWorkflow decompressXscrWorkflow)
    : IDecompressWorkflow
{
    public void Decompress()
    {
        bool isDirectory = Directory.Exists(config.InputPath);
        if (isDirectory)
            DecompressDirectory(config.InputPath);
        else
            DecompressFile(config.InputPath);
    }

    private void DecompressDirectory(string dirPath)
    {
        string[] files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

        foreach (string file in files)
            DecompressFile(file);
    }

    private void DecompressFile(string filePath)
    {
        Console.Write($"Decompress {filePath}... ");

        using Stream inputStream = File.OpenRead(filePath);
        using Stream outputStream = File.Create(filePath + ".dec");

        if (!TryPeekType(inputStream, out ScriptType? type))
        {
            Console.WriteLine("Unknown script type.");
            return;
        }

        if (type.Value is ScriptType.Gss1 or ScriptType.Gsd1)
        {
            Console.WriteLine("GSS1 and GSD1 scripts have no compression.");
            return;
        }

        bool wasSuccessful = TryDecompressFile(inputStream, outputStream, type.Value, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");
    }

    private bool TryDecompressFile(Stream input, Stream output, ScriptType type, [NotNullWhen(false)] out Exception? error)
    {
        error = null;

        try
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    decompressXq32Workflow.Decompress(input, output);
                    break;

                case ScriptType.Xseq:
                    decompressXseqWorkflow.Decompress(input, output);
                    break;

                case ScriptType.Xscr:
                    decompressXscrWorkflow.Decompress(input, output);
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