using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract.DataClasses
{
    public enum CompressionType
    {
        Level5_Lz10,
        Level5_Huffman4Bit,
        Level5_Huffman8Bit,
        Level5_Rle,
        ZLib
    }
}
