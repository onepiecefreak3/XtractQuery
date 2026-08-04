using Kryptography.Checksum;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.InternalContract.Checksum;
using System.Diagnostics.CodeAnalysis;

namespace Logic.Domain.Level5.Script.Xq32;

internal class Xq32FunctionCache(IChecksumFactory checksums) : IXq32FunctionCache
{
    private readonly Checksum<uint> _hash = checksums.CreateCrc32();

    private readonly Dictionary<uint, string> _lookup = [];

    public bool TryAdd(string scriptName, string name)
    {
        string fullName = scriptName.Replace('/', '.').Replace('\\', '.');
        fullName += $".{name}";

        return _lookup.TryAdd(_hash.ComputeValue(name), fullName);
    }

    public bool TryResolve(uint checksum, [NotNullWhen(true)] out string? functionName)
    {
        return _lookup.TryGetValue(checksum, out functionName);
    }
}