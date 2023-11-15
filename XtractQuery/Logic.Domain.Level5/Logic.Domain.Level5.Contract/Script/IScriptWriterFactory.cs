﻿using Logic.Domain.Level5.Contract.Script.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Exceptions;

namespace Logic.Domain.Level5.Contract.Script
{
    [MapException(typeof(ScriptWriterFactoryException))]
    public interface IScriptWriterFactory
    {
        IScriptWriter Create(ScriptType type);
    }
}
