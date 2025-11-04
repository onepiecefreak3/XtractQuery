using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using System.Diagnostics.CodeAnalysis;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.Level5.Contract.DataClasses.Script;

namespace Logic.Business.Level5ScriptManagement.Creation;

class CreateWorkflow(
    ScriptManagementConfiguration config,
    IScriptTypeConverter typeConverter,
    ICreateXq32Workflow createXq32Workflow,
    ICreateXseqWorkflow createXseqWorkflow,
    ICreateXscrWorkflow createXscrWorkflow,
    ICreateGss1Workflow createGss1Workflow,
    ICreateGsd1Workflow createGsd1Workflow,
    ICreateGdsWorkflow createGdsWorkflow)
    : ICreateWorkflow
{
    public void Create()
    {
        if (!typeConverter.TryConvert(config.QueryType, out ScriptType? type))
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

                case ScriptType.Xscr:
                    using (Stream outputStream = File.Create(filePath + ".xs"))
                        createXscrWorkflow.Create(inputStream, outputStream);
                    break;

                case ScriptType.Gss1:
                    using (Stream outputStream = File.Create(filePath + ".cq"))
                        createGss1Workflow.Create(inputStream, outputStream);
                    break;

                case ScriptType.Gsd1:
                    using (Stream outputStream = File.Create(filePath + ".lb"))
                        createGsd1Workflow.Create(inputStream, outputStream);
                    break;

                case ScriptType.Gds:
                    using (Stream outputStream = File.Create(filePath + ".gds"))
                        createGdsWorkflow.Create(inputStream, outputStream);
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

    private static Exception GetInnermostException(Exception e)
    {
        while (e.InnerException != null)
            e = e.InnerException;

        return e;
    }
}