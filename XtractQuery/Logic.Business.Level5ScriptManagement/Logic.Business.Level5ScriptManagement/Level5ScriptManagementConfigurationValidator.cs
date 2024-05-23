using Logic.Business.Level5ScriptManagement.InternalContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Business.Level5ScriptManagement.InternalContract.Exceptions;

namespace Logic.Business.Level5ScriptManagement
{
    internal class Level5ScriptManagementConfigurationValidator : ILevel5ScriptManagementConfigurationValidator
    {
        public void Validate(Level5ScriptManagementConfiguration config)
        {
            if (config.ShowHelp)
                return;

            ValidateOperation(config);
            ValidateFilePath(config);
            ValidatePointerLength(config);
            ValidateQueryType(config);
        }

        private void ValidateOperation(Level5ScriptManagementConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Operation))
                throw new Level5ScriptManagementConfigurationValidationException("No operation mode was given. Specify an operation mode by using the -o argument.");

            if (config.Operation != "e" && config.Operation != "c" && config.Operation != "d")
                throw new Level5ScriptManagementConfigurationValidationException($"The operation mode '{config.Operation}' is not valid. Use -h to see a list of valid operation modes.");
        }

        private void ValidateFilePath(Level5ScriptManagementConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.FilePath))
                throw new Level5ScriptManagementConfigurationValidationException("No file to process was specified. Specify a file by using the -f argument.");

            if (!File.Exists(config.FilePath) && !Directory.Exists(config.FilePath))
                throw new Level5ScriptManagementConfigurationValidationException($"File or directory '{Path.GetFullPath(config.FilePath)}' was not found.");
        }

        private void ValidatePointerLength(Level5ScriptManagementConfiguration config)
        {
            if (config.Operation != "c")
                return;

            if (string.IsNullOrWhiteSpace(config.Length))
                throw new Level5ScriptManagementConfigurationValidationException("No pointer length was given. Specify a pointer length by using the -l argument.");

            if (config.Length != "int" && config.Length != "long")
                throw new Level5ScriptManagementConfigurationValidationException($"The pointer length '{config.Length}' is not valid. Use -h to see a list of valid pointer lengths.");
        }

        private void ValidateQueryType(Level5ScriptManagementConfiguration config)
        {
            if (config.Operation != "c")
                return;

            if (string.IsNullOrWhiteSpace(config.QueryType))
                throw new Level5ScriptManagementConfigurationValidationException("No query type was given. Specify a query type by using the -t argument.");

            if (config.QueryType != "xq32" && config.QueryType != "xseq")
                throw new Level5ScriptManagementConfigurationValidationException($"The query type '{config.QueryType}' is not valid. Use -h to see a list of valid query types.");
        }
    }
}
