using Logic.Business.Level5ScriptManagement.Contract;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Business.Level5ScriptManagement.InternalContract.Decompression;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

namespace Logic.Business.Level5ScriptManagement;

internal class ScriptManagementWorkflow(
    ScriptManagementConfiguration config,
    IScriptManagementConfigurationValidator configValidator,
    IExtractWorkflow extractWorkflow,
    ICreateWorkflow createWorkflow,
    IDecompressWorkflow decompressWorkflow)
    : IScriptManagementWorkflow
{
    public int Execute()
    {
        if (config.ShowHelp || Environment.GetCommandLineArgs().Length <= 0)
        {
            PrintHelp();
            return 0;
        }

        if (!TryValidateConfig())
        {
            PrintHelp();
            return 0;
        }

        switch (config.Operation)
        {
            case "e":
                extractWorkflow.Extract();
                break;

            case "c":
                createWorkflow.Create();
                break;

            case "d":
                decompressWorkflow.Decompress();
                break;
        }

        return 0;
    }

    private bool TryValidateConfig()
    {
        try
        {
            configValidator.Validate(config);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine();

            return false;
        }

        return true;
    }

    private void PrintHelp()
    {
        Console.WriteLine("Following commands exist:");
        Console.WriteLine("  -h, --help\t\tShows this help message.");
        Console.WriteLine("  -o, --operation\tThe operation to take on the file");
        Console.WriteLine("    Valid operations are: e for extraction, c for creation, d for decompression");
        Console.WriteLine("  -t, --type\t\tThe type of file given");
        Console.WriteLine("    Valid types are: xq32, xseq, gss1, gsd1");
        Console.WriteLine("    The type is automatically detected when extracting; This argument will not have any effect on operation 'e' and 'd'");
        Console.WriteLine("  -f, --file\t\tThe file to process");
        Console.WriteLine("  -nc, --no-compression\t[Optional] If the file should use a compression layer");
        Console.WriteLine("    This option is automatically detected when extracting; This argument will not have any effect on operation 'e' and 'd'");
        Console.WriteLine("  -l, --length\t\t[Optional]The pointer length given");
        Console.WriteLine("    Valid lengths are: int, long");
        Console.WriteLine("    Default value is 'int'");
        Console.WriteLine("    The length is automatically detected when extracting; This argument will not have any effect on operation 'e' and scripts of type 'gss1'");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"\tExtract any script to human readable text: {Environment.ProcessPath} -o e -f Path/To/File.xq");
        Console.WriteLine($"\tCreate a xq32 script from human readable text: {Environment.ProcessPath} -o c -t xq32 -f Path/To/File.txt");
        Console.WriteLine($"\tCreate a xq32 script with long pointers from human readable text: {Environment.ProcessPath} -o c -t xq32 -l long -f Path/To/File.txt");
        Console.WriteLine($"\tCreate a xq32 script without a compression layer from human readable text: {Environment.ProcessPath} -o c -t xq32 -n -f Path/To/File.txt");
    }
}