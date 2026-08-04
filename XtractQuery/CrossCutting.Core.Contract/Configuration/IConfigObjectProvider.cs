using System;

namespace CrossCutting.Core.Contract.Configuration;

public interface IConfigObjectProvider
{
    TConfig Get<TConfig>();
    object Get(Type configType);
}