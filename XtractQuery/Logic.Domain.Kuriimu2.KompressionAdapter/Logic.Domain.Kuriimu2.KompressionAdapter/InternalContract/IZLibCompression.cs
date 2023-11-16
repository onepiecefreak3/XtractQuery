using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract
{
    [MapException(typeof(ZLibCompressionException))]
    public interface IZLibCompression : ICompression
    {
    }
}
