using NekoBot.Interfaces;
using NekoBot.Types;
using NekoBot.Types.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = NekoBot.Interfaces.ISerializer;
using Version = NekoBot.Types.Version;
public class YamlSerializer : Extension, IExtension, ISerializer
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "YamlSerializer",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Serializer
    };
    public string Serialize<T>(T obj)
    {
        var serializer = new SerializerBuilder().Build();
        return serializer.Serialize(obj);
    }
    public T? Deserialize<T>(string yaml)
    {
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<T>(yaml);
    }
}