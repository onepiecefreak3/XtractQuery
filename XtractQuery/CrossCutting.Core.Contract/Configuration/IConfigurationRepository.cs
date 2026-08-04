using System.Collections.Generic;
using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace CrossCutting.Core.Contract.Configuration;

public interface IConfigurationRepository
{
    IEnumerable<ConfigCategory> Load();
}