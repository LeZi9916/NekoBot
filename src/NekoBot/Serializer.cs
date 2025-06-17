using System.Text.Json;
using YamlDotNet.Serialization;

namespace NekoBot;
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
        public static string Serialize<T>(T obj)
        {
            var serializer = new SerializerBuilder().Build();
            return serializer.Serialize(obj);
        }
        public static T? Deserialize<T>(string yaml)
        {
            var deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<T>(yaml);
        }
    }
}
