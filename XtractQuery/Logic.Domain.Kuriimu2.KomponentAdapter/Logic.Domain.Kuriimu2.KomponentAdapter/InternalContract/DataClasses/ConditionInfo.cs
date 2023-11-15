using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses
{
    public class ConditionInfo
    {
        public string FieldName { get; }
        public ConditionComparer Comparer { get; }
        public ulong Value { get; }

        public ConditionInfo(string fieldName, ConditionComparer comp, ulong value)
        {
            FieldName = fieldName;
            Comparer = comp;
            Value = value;
        }
    }
}
