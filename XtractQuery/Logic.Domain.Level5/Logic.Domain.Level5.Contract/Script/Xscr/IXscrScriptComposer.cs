using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptComposer
{
    XscrScriptContainer Compose(XscrScriptFile script);
}