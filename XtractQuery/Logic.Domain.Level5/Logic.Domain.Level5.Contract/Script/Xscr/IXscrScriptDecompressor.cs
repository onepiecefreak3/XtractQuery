using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptDecompressor
{
    XscrScriptContainer Decompress(Stream input);
}