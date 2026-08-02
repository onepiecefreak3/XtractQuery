using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

class StatementBlock
{
    public IList<StatementBlock> Parents { get; } = [];

    public IList<StatementBlock> Children { get; } = [];

    public int InstructionIndex { get; set; } = -1;

    public int StatementCount { get; set; }

    public bool IsExit { get; set; }

    public HashSet<string> Labels { get; } = [];

    public IList<StatementSyntax> Statements { get; } = [];

    public int EndStatementIndex => InstructionIndex < 0 ? -1 : InstructionIndex + StatementCount;
}
