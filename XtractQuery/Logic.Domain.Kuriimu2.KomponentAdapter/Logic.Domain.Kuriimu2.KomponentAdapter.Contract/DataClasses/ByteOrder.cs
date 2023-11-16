using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    public enum ByteOrder
    {
        BigEndian = 0xFEFF,
        LittleEndian = 0xFFFE,
    }
}
