using System.Text.Json;
using YamlDotNet.Serialization;

namespace NekoBot.Text;
public static class Serializer
{
    public static class Json
    {
        public static string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
        public static string Serialize<T>(T obj, JsonSerializerOptions option) => System.Text.Json.JsonSerializer.Serialize(obj, option);
        public static T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);
        public static T? Deserialize<T>(string json, JsonSerializerOptions option) => System.Text.Json.JsonSerializer.Deserialize<T>(json, option);
    }
    public static class Yaml
    {
        static readonly ISerializer _serializer = new SerializerBuilder().Build();
        static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

        public static string Serialize<T>(T obj)
        {
            return _serializer.Serialize(obj);
        }
        public static T Deserialize<T>(string yaml)
        {
            return _deserializer.Deserialize<T>(yaml);
        }
    }
}
