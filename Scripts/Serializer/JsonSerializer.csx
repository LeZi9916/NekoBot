using NekoBot.Interfaces;
using NekoBot.Types;
using NekoBot.Types.Core;
using System.Text.Json;
using Version = NekoBot.Types.Version;
public class JsonSerializer : Extension, IExtension, ISerializer
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "JsonSerializer",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Serializer
    };
    public string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
    public string Serialize<T>(T obj, JsonSerializerOptions option) => System.Text.Json.JsonSerializer.Serialize(obj,option);
    public T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);
    public T? Deserialize<T>(string json,JsonSerializerOptions option) => System.Text.Json.JsonSerializer.Deserialize<T>(json,option);


}
