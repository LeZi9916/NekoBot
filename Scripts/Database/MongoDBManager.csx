using MongoDB.Driver;
using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Version = NekoBot.Types.Version;

public class MongoDBManager : Destroyable, IExtension, IDestroyable, IDBManager
{
    MongoClient? dbSession;
    DatabaseInfo info;
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "MongoDBManager",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Database
    };
    public override void Init()
    {
        base.Init();
        var _info = Core.Config.Database;
        if (_info is null)
            throw new InvalidOperationException("The configuration information for the database could not be found.");
        info = (DatabaseInfo)_info;
        if (info.Address is null)
            throw new InvalidOperationException("Invalid database address.");
        var port = info.Port ?? 27017;
        if(string.IsNullOrEmpty(info.Username) && string.IsNullOrEmpty(info.Password))
            dbSession = new MongoClient($"mongodb://{info.Address}:{port}");
        else
            dbSession = new MongoClient($"mongodb://{info.Username}:{info.Password}@{info.Address}:{port}");
    }
    public IDatabase<T> GetCollection<T>(string collectionName) => GetCollection<T>("NekoBotDB", collectionName);
    public IDatabase<T> GetCollection<T>(string dbName ,string collectionName)
    {
        if (dbSession is null)
            throw new InvalidOperationException("Database was not connect.");
        var db = dbSession.GetDatabase(dbName);
        var collection = db.GetCollection<T>(collectionName);
        return new Collection<T>(collection);
    }
    public override void Destroy()
    {
        base.Destroy();
        dbSession = null;
    }
}
public class Collection<T> :Destroyable, IDatabase<T>,IDestroyable
{
    IMongoCollection<T> collection;
    public int Count { get => (int)collection.CountDocuments(_ => true); }
    public Collection(IMongoCollection<T> collection)
    {
        this.collection = collection;
    }
    public T[] Export() => collection.Find(_ => true).ToArray();
    public void Import(T[] array)
    {
        collection.DeleteMany(_ => true);
        collection.InsertMany(array);
    }
    public void Insert(T obj, Expression<Func<T, bool>>? match = null)
    {
        if (match is not null && Find(match) is not null)
            Update(match, obj);
        else
            collection.InsertOne(obj);
    }
    public bool Remove(Expression<Func<T, bool>> match) => collection.DeleteOne(match).IsAcknowledged;
    public bool Update(Expression<Func<T, bool>> match,T obj)
    {
        var result = collection.ReplaceOne(match,obj);
        return result.IsAcknowledged;
    }
    public bool Exists(Expression<Func<T, bool>> match) => Find(match) is not null;
    public void Clear() => collection.DeleteMany(_ => true);
    public T? Find(Expression<Func<T, bool>> match) => collection.Find(match).FirstOrDefault();
    public List<T> FindAll(Expression<Func<T, bool>> match) => collection.Find(match).ToList();
    public T? FindLast(Expression<Func<T, bool>> match) => collection.Find(match).LastOrDefault();
    public IEnumerator<T> GetEnumerator() => GetEnumerator(collection.FindSync(Builders<T>.Filter.Empty));
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<T> GetEnumerator(IAsyncCursor<T> cursor) => cursor.Current.GetEnumerator();
}
