using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses
{
    public class BitFieldInfo
    {
        public int BlockSize = 4;
        public BitOrder BitOrder = BitOrder.Default;
    }
}
