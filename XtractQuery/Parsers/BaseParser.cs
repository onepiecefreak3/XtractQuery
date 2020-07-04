using System.IO;
using XtractQuery.Compression;
using XtractQuery.Interfaces;
using XtractQuery.Parsers.Models;

namespace XtractQuery.Parsers
{
    abstract class BaseParser: IParser
    {
        public abstract void Decompile(Stream input, Stream output);

        public abstract void Compile(Stream input, Stream output);

        protected Stream Decompress(Stream input, long offset)
        {
            var decompressedStream = new MemoryStream();

            input.Position = offset;
            Level5Compressor.Decompress(input, decompressedStream);

            decompressedStream.Position = 0;
            return decompressedStream;
        }

        protected Stream Compress(Stream input)
        {
            var bestStream = input;

            var length = input.Length;
            for (var i = 1; i < 5; i++)
            {
                var output = new MemoryStream();

                input.Position = 0;
                Level5Compressor.Compress(input, output, (Level5CompressionMethod)i);

                if (output.Length < length)
                {
                    length = output.Length;
                    bestStream = output;
                }
            }

            bestStream.Position = 0;
            return bestStream;
        }
    }
}
