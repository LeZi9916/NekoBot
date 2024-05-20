using NekoBot.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NekoBot.Types
{
    public class Database<T> : ExtensionCore, IDatabase<T>
    {
        protected string? dbPath;
        protected ISerializer? jsonSerializer;
        protected ISerializer? yamlSerializer;
        protected List<T> database = new();

        public int Count => database.Count;
        public bool IsReadOnly => ((ICollection<T>)database).IsReadOnly;
        public T this[int index]
        {
            get => database[index];
            set => database[index] = value;
        }

        public override void Init()
        {
            jsonSerializer = (ISerializer)ScriptManager.GetExtension("JsonSerializer")!;
            yamlSerializer = (ISerializer)ScriptManager.GetExtension("YamlSerializer")!;
            dbPath = Config.DatabasePath;
        }
        public override void Destroy() => Save();
        public bool Exists(Predicate<T> match) => FindIndex(match) != -1;
        public T? Find(Predicate<T> match)
        {
            foreach (var item in database)
                if (match(item))
                    return item;
            return default;
        }
        public List<T> FindAll(Predicate<T> match) => database.FindAll(match);
        public int FindIndex(Predicate<T> match) => database.FindIndex(match);
        public T? FindLast(Predicate<T> match) => database.FindLast(match);
        public int FindLastIndex(Predicate<T> match) => database.FindLastIndex(match);
        public virtual void Add(T item) => database.Add(item);
        public virtual bool Remove(T item) => database.Remove(item);
        public virtual void SetAll(T[] collection) => database = new(collection);
        public virtual T[] All() => database.ToArray();
        public int IndexOf(T item) => database.IndexOf(item);
        public void Insert(int index, T item) => database.Insert(index, item);
        public void RemoveAt(int index) => database.RemoveAt(index);
        public void Clear() => database.Clear();
        public bool Contains(T item) => database.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => database.CopyTo(array, arrayIndex);
        public bool Update(Predicate<T> match, T item) 
        {
            try
            {
                var originIndex = FindIndex(match);
                if (originIndex != -1)
                    database[originIndex] = item;
                else
                    database.Add(item);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public IEnumerator<T> GetEnumerator() => database.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => database.GetEnumerator();
        protected static T2 Load<T2>(ISerializer serializer, string path) where T2 : new()
        {
            try
            {
                var json = File.ReadAllText(path);
                var result = serializer.Deserialize<T2>(json);

                Debug(DebugType.Info, $"Loaded File: {path}");
                return result;
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Loading \"{path}\" Failure:\n{e.Message}");
                return new T2();
            }
        }
        protected static void Load<T2>(ISerializer serializer, string path, out T2 obj) where T2 : new() => obj = Load<T2>(serializer, path);
        protected static async void Save<T2>(ISerializer serializer, string path, T2 target, bool debugMessage = true)
        {
            try
            {
                var fileStream = File.Open(path, FileMode.Create);
                await fileStream.WriteAsync(Encoding.UTF8.GetBytes(serializer.Serialize(target)));
                fileStream.Close();
                if (debugMessage)
                    Debug(DebugType.Info, $"Saved File {path}");
            }
            catch (Exception e)
            {
                Debug(DebugType.Error, $"Saving File \"{path}\" Failure:\n{e.Message}");
            }
        }
    }
}
