using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Script.Xq32.InternalContract.Exceptions;

namespace Logic.Domain.Level5.Script.Xq32.InternalContract;

[MapException(typeof(Xq32StringTableException))]
public interface IXq32StringTable : IStringTable;