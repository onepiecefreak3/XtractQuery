using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.DataClasses
{
    public class TypeChoice
    {
        public string FieldName { get; }
        public TypeChoiceComparer Comparer { get; }
        public ulong Value { get; }
        public Type InjectionType { get; }

        public TypeChoice(string fieldName, TypeChoiceComparer comp, ulong value, Type injectionType)
        {
            FieldName = fieldName;
            Comparer = comp;
            Value = value;
            InjectionType = injectionType;
        }
    }
}
