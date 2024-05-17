using NekoBot.Interfaces;
using NekoBot.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = NekoBot.Interfaces.ISerializer;
using Version = NekoBot.Types.Version;
#nullable enable
namespace NekoBot.Scripts
{
    public class YamlSerializer : ExtensionCore, IExtension, ISerializer
    {
        public new ExtensionInfo Info { get; } = new ExtensionInfo()
        {
            Name = "YamlSerializer",
            Version = new Version() { Major = 1, Minor = 0 }
        };
        public static string Serialize<T>(T obj)
        {
            var serializer = new SerializerBuilder()
                                 .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                 .Build();
            return serializer.Serialize(obj);
        }
        public static T? Deserialize<T>(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                                   .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                   .Build();
            return deserializer.Deserialize<T>(yaml);
        }
    }
}
