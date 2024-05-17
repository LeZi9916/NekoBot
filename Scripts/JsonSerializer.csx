using NekoBot.Interfaces;
using NekoBot.Types;
using Version = NekoBot.Types.Version;

#nullable enable
namespace NekoBot.Scripts
{
    public class JsonSerializer : ExtensionCore, IExtension, ISerializer
    {
        public new ExtensionInfo Info { get; } = new ExtensionInfo()
        {
            Name = "JsonSerializer",
            Version = new Version() { Major = 1, Minor = 0 }
        };
        public static string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
        public static T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);


    }
}
