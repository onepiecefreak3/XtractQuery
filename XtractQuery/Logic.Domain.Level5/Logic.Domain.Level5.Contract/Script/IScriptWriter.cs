using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Exceptions;

namespace Logic.Domain.Level5.Contract.Script
{
    [MapException(typeof(ScriptWriterException))]
    public interface IScriptWriter
    {
        void Write(ScriptFile script, Stream output, bool hasCompression);
        void Write(ScriptFile script, Stream output, CompressionType compressionType);
        void Write(ScriptContainer container, Stream output, bool hasCompression);
        void Write(ScriptContainer container, Stream output, CompressionType compressionType);
    }
}
