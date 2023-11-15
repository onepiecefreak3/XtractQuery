using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.DataClasses
{
    public class ScriptFile
    {
        public IList<ScriptFunction> Functions { get; set; }
        public IList<ScriptJump> Jumps { get; set; }
        public IList<ScriptInstruction> Instructions { get; set; }
        public IList<ScriptArgument> Arguments { get; set; }
    }
}
