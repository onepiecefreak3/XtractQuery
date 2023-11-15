using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.DependencyInjection.Exceptions;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    [MapException(typeof(DependencyInjectionException))]
    public interface IKernelInitializer
    {
        void Initialize();
    }
}