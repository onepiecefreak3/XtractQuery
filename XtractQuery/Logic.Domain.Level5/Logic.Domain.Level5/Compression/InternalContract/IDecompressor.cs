using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Compression.InternalContract.Exceptions;
using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Compression.InternalContract
{
    [MapException(typeof(DecompressorException))]
    public interface IDecompressor
    {
        Stream Decompress(Stream input, long offset);
        CompressionType PeekCompressionType(Stream input, long offset);
    }
}
