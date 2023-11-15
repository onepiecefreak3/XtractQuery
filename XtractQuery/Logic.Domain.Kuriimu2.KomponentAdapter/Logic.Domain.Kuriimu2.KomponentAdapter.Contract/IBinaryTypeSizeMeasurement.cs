using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract
{
    [MapException(typeof(BinaryTypeSizeMeasurementException))]
    public interface IBinaryTypeSizeMeasurement
    {
        int Measure<T>();
        int Measure(Type type);
    }
}
