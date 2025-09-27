using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptReader
{
    XscrScriptContainer Read(Stream input);
    XscrScriptContainer Read(XscrCompressionContainer container);
}
