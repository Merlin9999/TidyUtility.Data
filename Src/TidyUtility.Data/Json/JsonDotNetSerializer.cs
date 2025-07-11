 #nullable disable
 using Newtonsoft.Json;
 using NodaTime;
 using NodaTime.Serialization.JsonNet;

 namespace TidyUtility.Data.Json
{
    public class JsonDotNetSerializer : ISerializer
    {
        public string Serialize<T>(T dataToSerialize)
        {
            return JsonConvert.SerializeObject(dataToSerialize, Formatting.None, GetJsonSerializerSettings());
        }

        public T Deserialize<T>(string serializedData)
        {
            return JsonConvert.DeserializeObject<T>(serializedData, GetJsonSerializerSettings());
        }

        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return settings;
        }
    }
}