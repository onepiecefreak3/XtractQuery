using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Exceptions;

namespace Logic.Domain.Level5.Contract.Script
{
    [MapException(typeof(StringTableException))]
    public interface IStringTable
    {
        Stream GetStream();

        string Read(long offset);
        long Write(string value);

        string GetByHash(uint hash);
        uint ComputeHash(string value);
    }
}
