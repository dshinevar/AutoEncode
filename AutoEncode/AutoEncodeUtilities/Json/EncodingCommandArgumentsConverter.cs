using AutoEncodeUtilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace AutoEncodeUtilities.Json
{
    public class EncodingCommandArgumentsConverter<T> : JsonConverter where T : IEncodingCommandArguments
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType.IsInterface && reader.TokenType != JsonToken.Null)
            {
                try
                {
                    JObject jsonObject = JObject.Load(reader);
                    string type = jsonObject["$type"].ToString();
                    var typesAsArray = type.Split(',');
                    var wrappedTarget = Activator.CreateInstance(typesAsArray[1], typesAsArray[0]);
                    var realTarget = wrappedTarget.Unwrap() as IEncodingCommandArguments;
                    serializer.Populate(jsonObject.CreateReader(), realTarget);
                    return realTarget;
                }
                catch (JsonReaderException) 
                {
                    // Fallback
                    return serializer.Deserialize<T>(reader);
                }
            }

            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (serializer.NullValueHandling != NullValueHandling.Ignore)
            {
                serializer.Serialize(writer, value, value.GetType());
            }
            writer.WriteEndObject();
        }
    }
}
