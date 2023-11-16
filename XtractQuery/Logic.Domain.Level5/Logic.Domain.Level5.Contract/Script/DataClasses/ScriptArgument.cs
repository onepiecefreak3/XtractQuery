using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.DataClasses
{
    public class ScriptArgument
    {
        public int RawArgumentType { get; set; }
        public ScriptArgumentType Type { get; set; }
        public object Value { get; set; }
    }
}
