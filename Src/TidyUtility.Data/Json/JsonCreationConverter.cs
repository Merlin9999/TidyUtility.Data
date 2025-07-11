using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TidyUtility.Data.Json
{
    // Adapted from: https://stackoverflow.com/a/8031283/677612
    public abstract class JsonCreationConverter<T> : JsonConverter
    {
        protected abstract T Create(Type objectType, JObject jObject);

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => false;

        public override object? ReadJson(JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            // Create target object based on JObject
            T target = this.Create(objectType, jObject);

            if (target == null) return null; // Should not happen

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }
    }
}
