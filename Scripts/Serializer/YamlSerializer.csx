using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using YamlDotNet.Serialization;
using ISerializer = NekoBot.Interfaces.ISerializer;
public class YamlSerializer : Extension, IExtension, ISerializer
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "YamlSerializer",
        Version = new Version("1.0"),
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