using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    public enum BitOrder
    {
        Default,
        MostSignificantBitFirst,
        LeastSignificantBitFirst,
        LowestAddressFirst,
        HighestAddressFirst
    }
}
