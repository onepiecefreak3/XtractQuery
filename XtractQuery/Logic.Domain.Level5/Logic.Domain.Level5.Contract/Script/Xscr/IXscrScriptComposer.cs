using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptComposer
{
    XscrScriptContainer Compose(XscrScriptFile script);
}