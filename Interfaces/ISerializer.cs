namespace NekoBot.Interfaces;

public interface ISerializer : IExtension
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string obj);
}
