using System.Collections.Generic;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    public interface IRequestContext
    {
        void ChangeParameters(Dictionary<string, object> parameter);
    }
}
