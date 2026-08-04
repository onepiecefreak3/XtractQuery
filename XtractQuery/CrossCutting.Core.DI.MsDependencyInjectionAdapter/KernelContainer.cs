using CrossCutting.Core.Contract.DependencyInjection;

namespace CrossCutting.Core.DI.MsDependencyInjectionAdapter;

public class KernelContainer : IKernelContainer
{
    private static ICoCoKernel? s_innerKernel;
    private static readonly object s_lock = new();

    public ICoCoKernel Kernel
    {
        get
        {
            lock (s_lock)
            {
                if (s_innerKernel is null)
                {
                    s_innerKernel = new KernelAdapter();
                    s_innerKernel.RegisterInstance(s_innerKernel);
                }

                return s_innerKernel;
            }
        }
    }
}
