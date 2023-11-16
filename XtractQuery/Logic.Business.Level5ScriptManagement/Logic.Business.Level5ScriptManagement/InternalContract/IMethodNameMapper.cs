using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Business.Level5ScriptManagement.InternalContract.Exceptions;

namespace Logic.Business.Level5ScriptManagement.InternalContract
{
    [MapException(typeof(MethodNameMapperException))]
    public interface IMethodNameMapper
    {
        bool MapsInstructionType(int instructionType);
        bool MapsMethodName(string methodName);

        string GetMethodName(int instructionType);
        int GetInstructionType(string methodName);
    }
}
