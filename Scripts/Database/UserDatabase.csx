using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
using Version = NekoBot.Types.Version;

public class UserDatabase : Database<User>, IExtension, IDatabase<User>, IDestroyable
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
    public override void Init()
    {
        base.Init();
        dbPath = Path.Combine(Config.DatabasePath, "UserDatabase.yaml");
        _database = Load<List<User>>(yamlSerializer!, dbPath);
        AutoSave();
    }
    public override void Add(User item) => Update(x => x.Id == item.Id, item);
    public override void Destroy()
    {
        base.Destroy();
        Save();
    }
}
