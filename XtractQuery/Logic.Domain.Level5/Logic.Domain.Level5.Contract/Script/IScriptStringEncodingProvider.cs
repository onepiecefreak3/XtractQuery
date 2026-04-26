using System.Text;
using Logic.Domain.Level5.Contract.Enums.Script;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptStringEncodingProvider
{
    Encoding GetEncoding();
    void SetEncoding(StringEncoding encoding);
}