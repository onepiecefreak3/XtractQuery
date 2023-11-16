using CrossCutting.Core.Contract.DependencyInjection;

namespace CrossCutting.Core.DI.AutofacAdapter
{
    public class KernelContainer : IKernelContainer
    {
        private static ICoCoKernel s_innerKernel;
        private static readonly object s_lock = new object();

        public ICoCoKernel Kernel
        {
            get
            {
                lock (s_lock)
                {
                    if (s_innerKernel == null)
                    {
                        s_innerKernel = new KernelAdapter(new Autofac.ContainerBuilder());
                        s_innerKernel.RegisterInstance(s_innerKernel);
                    }

                    return s_innerKernel;
                }
            }
        }
    }
}
