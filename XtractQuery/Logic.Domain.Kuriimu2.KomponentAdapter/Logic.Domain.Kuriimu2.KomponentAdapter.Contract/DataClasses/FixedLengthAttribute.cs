using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FixedLengthAttribute : Attribute
    {
        public int Length { get; }
        public StringEncoding StringEncoding = StringEncoding.ASCII;

        public FixedLengthAttribute(int length)
        {
            Length = length;
        }
    }
}
