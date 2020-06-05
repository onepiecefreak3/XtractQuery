using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using XtractQuery.IO;
using XtractQuery.Parser2.Models;

namespace XtractQuery.Parser2
{
    class XseqParser
    {
        public XseqParser(Stream input)
        {
            Parse(input);
        }

        private void Parse(Stream input)
        {
            using (var br = new BinaryReaderY(input))
            {
                var header = br.ReadStruct<XseqHeader>();
            }
        }
    }
}
