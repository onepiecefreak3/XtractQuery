using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Kompression;
using Kompression.Implementations;

namespace Logic.Domain.Kuriimu2.KompressionAdapter
{
    internal class ZLibCompression : IZLibCompression
    {
        private readonly ICompression _compression;

        public ZLibCompression()
        {
            _compression = Compressions.ZLib.Build();
        }

        public void Decompress(Stream input, Stream output)
        {
            _compression.Decompress(input, output);
        }

        public void Compress(Stream input, Stream output)
        {
            _compression.Compress(input, output);
        }
    }
}
