namespace NekoBot.Interfaces;

public interface ISerializer : IExtension
{
    abstract static string Serialize<T>(T obj);
    abstract static T Deserialize<T>(string obj);
}
