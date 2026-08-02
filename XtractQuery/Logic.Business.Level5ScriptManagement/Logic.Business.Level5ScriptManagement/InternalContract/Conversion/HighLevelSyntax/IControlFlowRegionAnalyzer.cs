using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;

internal interface IControlFlowRegionAnalyzer
{
    ControlFlowRegions Analyze(ControlFlowGraph cfg);
}
