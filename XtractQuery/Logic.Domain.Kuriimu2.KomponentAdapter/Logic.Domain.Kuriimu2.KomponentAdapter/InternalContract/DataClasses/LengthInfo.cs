using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses
{
    public class LengthInfo
    {
        public int Length { get; }
        public Encoding Encoding { get; }

        public LengthInfo(int length, Encoding encoding)
        {
            Length = length;
            Encoding = encoding;
        }
    }
}
