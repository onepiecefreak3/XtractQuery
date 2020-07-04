using System.IO;

namespace XtractQuery.Interfaces
{
    interface IParser
    {
        void Decompile(Stream input, Stream output);
        void Compile(Stream input, Stream output);
    }
}
