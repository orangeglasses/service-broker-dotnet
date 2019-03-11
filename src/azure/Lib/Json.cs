using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace azure.Lib
{
    internal class Json : IJson
    {
        public JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        public T Deserialize<T>(JsonReader jsonReader)
        {
            return JsonSerializer.Deserialize<T>(jsonReader);
        }

        private static JsonSerializer JsonSerializer => new JsonSerializer
        {
            ContractResolver = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            }.ContractResolver,
            Converters = { new StringEnumConverter() }
        };
    }
}
