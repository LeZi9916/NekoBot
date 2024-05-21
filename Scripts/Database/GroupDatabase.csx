using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;

public class GroupDatabase : Database<Group>, IExtension, IDatabase<Group>
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "GroupDatabase",
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
        _database = Load<List<Group>>(yamlSerializer!, Path.Combine(dbPath!, "GroupDatabase.yaml"));
        AutoSave();
    }
    public override void Save() 
    {
        Save(yamlSerializer!, Path.Combine(dbPath!, "GroupDatabase.yaml"), _database);
    }
    public override void Add(Group item) => Update(x => x.Id == item.Id, item);
    public override void Destroy()
    {
        base.Destroy();
        Save();
    }
}
