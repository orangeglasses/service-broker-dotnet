using Newtonsoft.Json;

namespace azure.Lib
{
    internal interface IJson
    {
        JsonSerializerSettings JsonSerializerSettings { get; }

        T Deserialize<T>(JsonReader jsonReader);
    }
}