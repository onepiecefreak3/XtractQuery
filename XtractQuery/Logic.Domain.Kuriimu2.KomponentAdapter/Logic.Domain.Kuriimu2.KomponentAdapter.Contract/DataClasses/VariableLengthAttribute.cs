using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VariableLengthAttribute : Attribute
    {
        public string FieldName { get; }
        public StringEncoding StringEncoding { get; set; } = StringEncoding.ASCII;
        public int Offset { get; set; }

        public VariableLengthAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
