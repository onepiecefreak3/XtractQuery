using System;
using System.Collections.Generic;
using System.Text;

namespace XtractQuery.Options
{
    class Argument
    {
        public static Argument Empty => new Argument(false);

        public bool Exists { get; }
        public IList<string> Values { get; }

        public Argument(bool exists)
        {
            Exists = exists;
            Values = Array.Empty<string>();
        }

        public Argument(bool exists, IList<string> values)
        {
            Exists = exists;
            Values = values ?? Array.Empty<string>();
        }
    }
}
