using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Compression.DataClasses
{
    public enum CompressionType
    {
        None = 0,
        Lz10,
        Huffman4Bit,
        Huffman8Bit,
        Rle,
        ZLib
    }
}
