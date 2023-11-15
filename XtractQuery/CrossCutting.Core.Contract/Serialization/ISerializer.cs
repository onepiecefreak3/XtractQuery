namespace CrossCutting.Core.Contract.Serialization
{
    public interface ISerializer
    {
        string Serialize<T>(T obj);

        T Deserialize<T>(string serializedText);
    }
}