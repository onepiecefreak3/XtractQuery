namespace Logic.Domain.Level5.Contract.Script;

public interface IStringTable
{
    Stream GetStream();

    string Read(long offset);
    long Write(string value);

    IList<string> GetByHash(uint hash);
    uint ComputeHash(string value);
}