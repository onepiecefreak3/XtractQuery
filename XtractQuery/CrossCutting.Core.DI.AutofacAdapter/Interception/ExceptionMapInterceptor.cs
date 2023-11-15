using System;
using System.Linq;
using System.Reflection;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using CrossCutting.Core.Contract.Aspects;

namespace CrossCutting.Core.DI.AutofacAdapter.Interception
{
    public class ExceptionMapInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Type interceptedType = invocation.TargetType;
            Type interfaceWithMappingAttributes =
                interceptedType.GetInterfaces().FirstOrDefault(i => i.GetAttribute<MapExceptionAttribute>() != null);
            if (interfaceWithMappingAttributes != null)
            {
                MapExceptionAttribute attribute = interfaceWithMappingAttributes.GetCustomAttribute<MapExceptionAttribute>();
                string typeMessage = attribute.Message;
                Type targetExceptionType = attribute.TargetException;

                try
                {
                    invocation.Proceed();
                }
                catch (Exception e) when (!e.GetType().IsSubclassOf(targetExceptionType) && e.GetType() != targetExceptionType)
                {
                    string methodMessage = invocation.Method.GetCustomAttribute<ExceptionMessageAttribute>()?.Message;

                    if (methodMessage != null)
                    {
                        methodMessage = string.Format(methodMessage, invocation.Arguments);
                    }

                    Exception exceptionInstance = (Exception)Activator.CreateInstance(targetExceptionType, methodMessage ?? typeMessage, e);
                    throw exceptionInstance;
                }
            }
        }
    }
}
