using System.Text;
using Logic.Domain.Level5.Contract.Enums.Script;
using Logic.Domain.Level5.Contract.Script;

namespace Logic.Domain.Level5.Script
{
    internal class ScriptStringEncodingProvider : IScriptStringEncodingProvider
    {
        private readonly Encoding _sjisEncoding = Encoding.GetEncoding("Shift-JIS");
        private readonly Encoding _utf8Encoding = Encoding.UTF8;

        private Encoding _encoding;

        public ScriptStringEncodingProvider()
        {
            _encoding = _sjisEncoding;
        }

        public Encoding GetEncoding()
        {
            return _encoding;
        }

        public void SetEncoding(StringEncoding encoding)
        {
            _encoding = encoding switch
            {
                StringEncoding.Sjis => _sjisEncoding,
                StringEncoding.Utf8 => _utf8Encoding,
                _ => throw new InvalidOperationException($"Unsupported string encoding {encoding}.")
            };
        }
    }
}
