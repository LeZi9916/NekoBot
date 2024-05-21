using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;

public class MaiDatabase : Database<MaiAccount>, IExtension, IDatabase<MaiAccount>
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "MaiDatabase",
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
        _database = Load<List<MaiAccount>>(yamlSerializer!, Path.Combine(dbPath!, "MaiDatabase.yaml"));
        AutoSave();
    }
    public override void Save()
    {
        Save(yamlSerializer!, Path.Combine(dbPath!, "MaiDatabase.yaml"), _database);
    }
    public override void Destroy()
    {
        base.Destroy();
        Save();
    }
}
