using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.DataClasses
{
    public class ScriptContainer
    {
        public ScriptTable FunctionTable { get; set; }
        public ScriptTable JumpTable { get; set; }
        public ScriptTable InstructionTable { get; set; }
        public ScriptTable ArgumentTable { get; set; }
        public ScriptStringTable StringTable { get; set; }

        public int GlobalVariableCount { get; set; }
    }
}
