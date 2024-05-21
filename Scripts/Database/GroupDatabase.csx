using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;

public class GroupDatabase : Database<Group>, IExtension, IDatabase<Group>,IDestroyable
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
    public override void Init()
    {
        base.Init();
        dbPath = Path.Combine(Config.DatabasePath, "GroupDatabase.yaml");
        _database = Load<List<Group>>(yamlSerializer!, dbPath);
        AutoSave();
    }
    public override void Add(Group item) => Update(x => x.Id == item.Id, item);
    public override void Destroy()
    {
        base.Destroy();
        Save();
    }
}
