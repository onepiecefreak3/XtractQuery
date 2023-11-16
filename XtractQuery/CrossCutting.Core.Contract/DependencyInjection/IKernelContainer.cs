using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.DependencyInjection.Exceptions;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    [MapException(typeof(DependencyInjectionException))]
    public interface IKernelContainer
    {
        ICoCoKernel Kernel { get; }
    }
}