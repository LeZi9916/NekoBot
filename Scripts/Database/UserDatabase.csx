using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;

public class UserDatabase : Database<User>, IExtension, IDatabase<User>
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "UserDatabase",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Database,
        Dependencies = new ExtensionInfo[]{
            new ExtensionInfo()
            {
                Name = "JsonSerializer",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Serializer
            },
            new ExtensionInfo()
            {
                Name = "YamlSerializer",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Serializer
            }
        }
    };
    async void AutoSave()
    {
        if (!Core.Config.DbAutoSave)
            return;
        var token = isDestroying.Token;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (hasChange)
            {
                Debug(DebugType.Info, $"[{Info.Name}] Auto saving...");
                Save();
                hasChange = false;
            }
            await Task.Delay(Core.Config.AutoSaveInterval * 1000, token);
        }
    }
    public override void Init()
    {
        base.Init();
        _database = Load<List<User>>(yamlSerializer!, Path.Combine(dbPath!, "UserDatabase.yaml"));
        AutoSave();
    }
    public override void Save()
    {
        Save(yamlSerializer!, Path.Combine(dbPath!, "UserDatabase.yaml"), _database);
    }
    public override void Add(User item) => Update(x => x.Id == item.Id, item);
    public override void Destroy()
    {
        base.Destroy();
        Save();
    }
}
