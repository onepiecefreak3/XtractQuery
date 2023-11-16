using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.DataClasses
{
    public class ScriptInstruction
    {
        public short ArgumentIndex { get; set; }
        public short ArgumentCount { get; set; }

        public short ReturnParameter { get; set; }

        public short Type { get; set; }
    }
}
