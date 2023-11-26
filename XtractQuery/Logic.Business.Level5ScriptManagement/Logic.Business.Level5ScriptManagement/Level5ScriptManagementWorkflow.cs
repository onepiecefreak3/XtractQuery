using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Business.Level5ScriptManagement.Contract;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Business.Level5ScriptManagement
{
    internal class Level5ScriptManagementWorkflow : ILevel5ScriptManagementWorkflow
    {
        private readonly Level5ScriptManagementConfiguration _config;
        private readonly ILevel5ScriptManagementConfigurationValidator _configValidator;
        private readonly ILevel5ScriptFileConverter _scriptConverter;
        private readonly ILevel5CodeUnitConverter _treeConverter;
        private readonly IScriptTypeReader _typeReader;
        private readonly IScriptReaderFactory _readerFactory;
        private readonly IScriptWriterFactory _writerFactory;
        private readonly IScriptDecompressorFactory _decompressorFactory;
        private readonly IScriptCompressorFactory _compressorFactory;
        private readonly ILevel5ScriptParser _scriptParser;
        private readonly ILevel5ScriptComposer _scriptComposer;
        private readonly ILevel5ScriptWhitespaceNormalizer _whiteSpaceNormalizer;

        public Level5ScriptManagementWorkflow(Level5ScriptManagementConfiguration config, ILevel5ScriptManagementConfigurationValidator configValidator,
            ILevel5ScriptFileConverter scriptConverter, ILevel5CodeUnitConverter treeConverter,
            IScriptTypeReader typeReader, IScriptReaderFactory readerFactory, IScriptWriterFactory writerFactory,
            IScriptDecompressorFactory decompressorFactory, IScriptCompressorFactory compressorFactory,
            ILevel5ScriptParser scriptParser, ILevel5ScriptComposer scriptComposer, ILevel5ScriptWhitespaceNormalizer whiteSpaceNormalizer)
        {
            _config = config;
            _configValidator = configValidator;
            _scriptConverter = scriptConverter;
            _treeConverter = treeConverter;
            _typeReader = typeReader;
            _readerFactory = readerFactory;
            _writerFactory = writerFactory;
            _decompressorFactory = decompressorFactory;
            _compressorFactory = compressorFactory;
            _scriptParser = scriptParser;
            _scriptComposer = scriptComposer;
            _whiteSpaceNormalizer = whiteSpaceNormalizer;
        }

        public int Execute()
        {
            if (_config.ShowHelp || Environment.GetCommandLineArgs().Length <= 0)
            {
                PrintHelp();
                return 0;
            }

            _configValidator.Validate(_config);

            switch (_config.Operation)
            {
                case "e":
                    ExtractScripts();
                    break;

                case "c":
                    CreateScripts();
                    break;

                case "d":
                    DecompressScripts();
                    break;
            }

            return 0;
        }

        private void ExtractScripts()
        {
            // Populate global string hash cache
            PopulateStringHashCache();

            // Collect files to extract
            bool isDirectory = Directory.Exists(_config.FilePath);
            string[] files = isDirectory ?
                Directory.GetFiles(_config.FilePath, "*.xq", SearchOption.AllDirectories) :
                new[] { _config.FilePath };

            // Extract files
            foreach (string file in files)
            {
                Console.Write($"Extract {file}: ");
                try
                {
                    ExtractScript(file);
                    Console.WriteLine("Ok");
                }
                catch (Exception e)
                {
                    string relativePath = isDirectory ?
                        Path.GetRelativePath(_config.FilePath, file) :
                        Path.GetFileName(file);
                    Console.WriteLine($"Could not extract {relativePath}: {GetInnermostException(e).Message}");
                }
            }
        }

        private void ExtractScript(string filePath)
        {
            // Read script data
            using Stream fileStream = File.OpenRead(filePath);

            ScriptType type = _typeReader.Peek(fileStream);
            IScriptReader scriptReader = _readerFactory.Create(type);

            ScriptFile script = scriptReader.Read(fileStream);

            // Convert to readable script
            CodeUnitSyntax codeUnit = _scriptConverter.CreateCodeUnit(script);
            _whiteSpaceNormalizer.NormalizeCodeUnit(codeUnit);

            string readableScript = _scriptComposer.ComposeCodeUnit(codeUnit);

            // Write readable script
            using Stream newFileStream = File.Create(filePath + ".txt");
            using StreamWriter streamWriter = new(newFileStream);

            streamWriter.Write(readableScript);
        }

        private void CreateScripts()
        {
            // Collect files to extract
            bool isDirectory = Directory.Exists(_config.FilePath);
            string[] files = isDirectory ?
                Directory.GetFiles(_config.FilePath, "*.txt", SearchOption.AllDirectories) :
                new[] { _config.FilePath };

            // Extract files
            foreach (string file in files)
            {
                Console.Write($"Compile {file}: ");
                try
                {
                    CreateScript(file);
                    Console.WriteLine("Ok");
                }
                catch (Exception e)
                {
                    string relativePath = isDirectory ?
                        Path.GetRelativePath(_config.FilePath, file) :
                        Path.GetFileName(file);
                    Console.WriteLine($"Could not compile {relativePath}: {GetInnermostException(e).Message}");
                }
            }
        }

        private void CreateScript(string filePath)
        {
            // Read readable script
            using Stream fileStream = File.OpenRead(filePath);
            using StreamReader streamReader = new(fileStream);

            string readableScript = streamReader.ReadToEnd();

            // Convert to script data
            CodeUnitSyntax codeUnit = _scriptParser.ParseCodeUnit(readableScript);

            ScriptType type = DetermineScriptType(_config.QueryType);
            ScriptFile script = _treeConverter.CreateScriptFile(codeUnit, type);

            // Write script data
            IScriptWriter scriptWriter = _writerFactory.Create(type);

            using Stream newFileStream = File.Create(filePath + ".xq");

            scriptWriter.Write(script, newFileStream, CompressionType.None);
        }

        private void DecompressScripts()
        {
            // Collect files to extract
            bool isDirectory = Directory.Exists(_config.FilePath);
            string[] files = isDirectory ?
                Directory.GetFiles(_config.FilePath, "*.xq", SearchOption.AllDirectories) :
                new[] { _config.FilePath };

            // Extract files
            foreach (string file in files)
            {
                Console.Write($"Decompress {file}: ");
                try
                {
                    DecompressScript(file);
                    Console.WriteLine("Ok");
                }
                catch (Exception e)
                {
                    string relativePath = isDirectory ?
                        Path.GetRelativePath(_config.FilePath, file) :
                        Path.GetFileName(file);
                    Console.WriteLine($"Could not decompress {relativePath}: {GetInnermostException(e).Message}");
                }
            }
        }

        private void DecompressScript(string filePath)
        {
            // Decompress script data
            using Stream fileStream = File.OpenRead(_config.FilePath);

            ScriptType type = _typeReader.Peek(fileStream);
            IScriptDecompressor decompressor = _decompressorFactory.Create(type);

            ScriptContainer container = decompressor.Decompress(fileStream);

            // Write script data
            using Stream newFileStream = File.Create(_config.FilePath + ".dec");

            IScriptCompressor compressor = _compressorFactory.Create(type);

            compressor.Compress(container, newFileStream, CompressionType.None);
        }

        private void PopulateStringHashCache()
        {
            string referenceDir = _config.ReferenceScriptPath;
            if (string.IsNullOrEmpty(referenceDir))
                return;

            referenceDir = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), referenceDir);
            if (!Directory.Exists(referenceDir))
                return;

            foreach (string scriptFile in Directory.GetFiles(referenceDir, "*.xq", SearchOption.AllDirectories))
            {
                using Stream scriptStream = File.OpenRead(scriptFile);
                ScriptType type = _typeReader.Peek(scriptStream);

                IScriptDecompressor decompressor = _decompressorFactory.Create(type);
                IScriptReader reader = _readerFactory.Create(type);

                ScriptTable functionTable = decompressor.DecompressFunctions(scriptStream);
                ScriptStringTable stringTable = decompressor.DecompressStrings(scriptStream);

                reader.ReadFunctions(functionTable, stringTable);
            }
        }

        private ScriptType DetermineScriptType(string type)
        {
            switch (type)
            {
                case "xq32":
                    return ScriptType.Xq32;

                case "xseq":
                    return ScriptType.Xseq;

                default:
                    throw new InvalidOperationException($"Unsupported script type {type}");
            }
        }

        private void PrintHelp()
        {
            Console.WriteLine("Following commands exist:");
            Console.WriteLine("  -h, --help\t\tShows this help message.");
            Console.WriteLine("  -o, --operation\tThe operation to take on the file");
            Console.WriteLine("    Valid operations are: e for extraction, c for creation, d for decompression");
            Console.WriteLine("  -t, --type\t\tThe type of file given");
            Console.WriteLine("    Valid types are: xq32, xseq");
            Console.WriteLine("    The type is automatically detected when extracting; This argument will not have any effect on operation 'e'");
            Console.WriteLine("  -f, --file\t\tThe file to process");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine($"\tExtract any script to human readable text: {Environment.ProcessPath} -o e -f Path/To/File.xq");
            Console.WriteLine($"\tCreate a xq32 script from human readable text: {Environment.ProcessPath} -o c -t xq32 -f Path/To/File.txt");
        }

        private Exception GetInnermostException(Exception e)
        {
            while (e.InnerException != null)
                e = e.InnerException;

            return e;
        }
    }
}
