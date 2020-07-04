using System;

namespace XtractQuery.Interfaces
{
    interface IStringReader : IDisposable
    {
        string Read(long offset);

        string GetByHash(uint hash);
    }
}
