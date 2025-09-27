using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptWriter
{
    void Write(XscrScriptFile script, Stream output);

    void Write(XscrScriptContainer container, Stream output);
}