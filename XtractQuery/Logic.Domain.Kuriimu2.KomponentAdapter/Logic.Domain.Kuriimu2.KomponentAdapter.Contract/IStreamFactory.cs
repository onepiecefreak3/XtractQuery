using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract
{
    [MapException(typeof(StreamFactoryException))]
    public interface IStreamFactory
    {
        Stream CreateSubStream(Stream baseStream, long offset);
        Stream CreateSubStream(Stream baseStream, long offset, long length);
    }
}
