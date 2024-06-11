
namespace NekoBot.Interfaces;
public interface IDBManager : IDestroyable
{
    IDatabase<T> GetCollection<T>(string dbName);
    IDatabase<T> GetCollection<T>(string dbName, string collectionName);
}
