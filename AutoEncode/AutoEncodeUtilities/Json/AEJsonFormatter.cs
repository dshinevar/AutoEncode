using H.Formatters;
using Newtonsoft.Json;
using System.Text;

namespace AutoEncodeUtilities.Json
{
    /// <summary>Custom JsonFormatter derived from <see cref="H.Formatters.NewtonsoftJsonFormatter"/></summary>
    public class AEJsonFormatter : NewtonsoftJsonFormatter
    {
        private readonly JsonSerializerSettings serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public AEJsonFormatter() { }

        protected override byte[] SerializeInternal(object obj)
        {
            string serializedObject = JsonConvert.SerializeObject(obj, serializerSettings);
            return Encoding.GetBytes(serializedObject);
        }

        protected override T DeserializeInternal<T>(byte[] bytes)
        {
            string jsonString = Encoding.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(jsonString, serializerSettings);
        }
    }
}
