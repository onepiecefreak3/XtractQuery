using System.Diagnostics.CodeAnalysis;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script;

namespace Logic.Business.Level5ScriptManagement.Extraction;

class ExtractWorkflow(
    ScriptManagementConfiguration config,
    IScriptTypeReader typeReader,
    IScriptTypeConverter typeConverter,
    IExtractXq32Workflow extractXq32Workflow,
    IExtractXseqWorkflow extractXseqWorkflow,
    IExtractXscrWorkflow extractXscrWorkflow,
    IExtractGss1Workflow extractGss1Workflow,
    IExtractGsd1Workflow extractGsd1Workflow,
    IExtractGdsWorkflow extractGdsWorkflow)
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
        string[] files = CollectFiles(dirPath);

        foreach (string file in files)
            ExtractFile(file);
    }

    private static string[] CollectFiles(string dirPath)
    {
        IEnumerable<string> files = Directory.EnumerateFiles(dirPath, "*.xq", SearchOption.AllDirectories);
        files = files.Concat(Directory.EnumerateFiles(dirPath, "*.xs", SearchOption.AllDirectories));
        files = files.Concat(Directory.EnumerateFiles(dirPath, "*.cq", SearchOption.AllDirectories));
        files = files.Concat(Directory.EnumerateFiles(dirPath, "*.lb", SearchOption.AllDirectories));
        files = files.Concat(Directory.EnumerateFiles(dirPath, "*.gds", SearchOption.AllDirectories));

        return [.. files];
    }

    private void ExtractFile(string filePath)
    {
        Console.Write($"Extract {filePath}... ");

        using Stream inputStream = File.OpenRead(filePath);

        string outputPath = filePath + ".txt";

        if (!TryPeekType(inputStream, out ScriptType? type))
        {
            switch (Path.GetExtension(filePath))
            {
                case ".gds":
                    type = ScriptType.Gds;
                    break;

                default:
                    if (!typeConverter.TryConvert(config.QueryType, out type))
                    {
                        Console.WriteLine("Script format could not be automatically determined. Set it explicitly with the -t parameter.");
                        return;
                    }
                    break;
            }
        }

        using Stream outputStream = File.Create(outputPath);

        bool wasSuccessful = TryExtractFile(inputStream, outputStream, type.Value, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");

        outputStream.Close();
        File.Delete(outputPath);
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

                case ScriptType.Xscr:
                    extractXscrWorkflow.Extract(input, output);
                    break;

                case ScriptType.Gss1:
                    extractGss1Workflow.Prepare();
                    extractGss1Workflow.Extract(input, output);
                    break;

                case ScriptType.Gsd1:
                    extractGsd1Workflow.Extract(input, output);
                    break;

                case ScriptType.Gds:
                    extractGdsWorkflow.Extract(input, output);
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