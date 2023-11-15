using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConditionAttribute : Attribute
    {
        public string FieldName { get; }
        public ConditionComparer Comparer { get; }
        public ulong Value { get; }

        public ConditionAttribute(string fieldName, ConditionComparer comp, ulong value)
        {
            FieldName = fieldName;
            Comparer = comp;
            Value = value;
        }
    }
}
