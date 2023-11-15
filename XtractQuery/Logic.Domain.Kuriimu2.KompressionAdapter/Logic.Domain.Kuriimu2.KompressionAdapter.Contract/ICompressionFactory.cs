using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract
{
    [MapException(typeof(CompressionFactoryException))]
    public interface ICompressionFactory
    {
        ICompression Create(CompressionType type);
    }
}
