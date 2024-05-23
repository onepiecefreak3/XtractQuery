using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.DataClasses
{
    public class ScriptStringTable
    {
        public CompressionType? CompressionType { get; set; }
        public Stream Stream { get; set; }
    }
}
