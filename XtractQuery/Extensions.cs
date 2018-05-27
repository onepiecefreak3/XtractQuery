using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XtractQuery.Compression;
using XtractQuery.IO;
using System.IO;

namespace XtractQuery
{
    public static class Extensions
    {
        public static void WriteMultipleCompressed<T>(this BinaryWriterY bw, IEnumerable<T> list, Level5.Method comp)
        {
            if (list.Count() <= 0)
            {
                bw.Write(0);
                return;
            }

            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterY(ms, true))
                foreach (var t in list)
                    bwIntern.WriteStruct(t);
            bw.Write(Level5.Compress(ms, comp));
        }

        public static void WriteStringsCompressed(this BinaryWriterY bw, IEnumerable<string> list, Level5.Method comp, Encoding enc)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterY(ms, true))
                foreach (var t in list)
                {
                    bwIntern.Write(enc.GetBytes(t));
                    bwIntern.Write((byte)0);
                }
            bw.Write(Level5.Compress(ms, comp));
        }
    }
}
