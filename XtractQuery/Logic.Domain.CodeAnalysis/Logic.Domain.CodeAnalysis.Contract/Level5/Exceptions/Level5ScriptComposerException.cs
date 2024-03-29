﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions
{
    public class Level5ScriptComposerException:Exception
    {
        public Level5ScriptComposerException()
        {
        }

        public Level5ScriptComposerException(string message) : base(message)
        {
        }

        public Level5ScriptComposerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5ScriptComposerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
