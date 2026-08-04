namespace CrossCutting.Core.Contract.DependencyInjection;

public interface IKernelContainer
{
    ICoCoKernel Kernel { get; }
}