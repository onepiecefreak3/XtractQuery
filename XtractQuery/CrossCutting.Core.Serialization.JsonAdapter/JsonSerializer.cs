using CrossCutting.Core.Contract.Serialization;
using Newtonsoft.Json;

namespace CrossCutting.Core.Serialization.JsonAdapter
{
    public class JsonSerializer : ISerializer
    {
        public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

        public T Deserialize<T>(string serializedText) => JsonConvert.DeserializeObject<T>(serializedText);
    }
}
