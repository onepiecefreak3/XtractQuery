using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CalculateLengthAttribute : Attribute
    {
        public Type CalculationType { get; }
        public string CalculationMethodName { get; }

        public StringEncoding StringEncoding { get; set; } = StringEncoding.ASCII;

        public CalculateLengthAttribute(Type calculationType, string calculationMethod)
        {
            CalculationType = calculationType;
            CalculationMethodName = calculationMethod;
        }
    }
}
