using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

class StatementBlock
{
    public IList<StatementBlock> Parents { get; set; } = [];

    public IList<StatementBlock> Children { get; set; } = [];

    public int InstructionIndex { get; set; } = -1;

    public bool IsExit { get; set; }

    public HashSet<string> Labels { get; set; } = [];

    public IList<StatementSyntax> Statements { get; set; } = [];
}