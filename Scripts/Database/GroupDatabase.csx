using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
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
    public override void Init()
    {
        base.Init();
        database = Load<List<Group>>(yamlSerializer!, Path.Combine(dbPath!, "GroupDatabase.yaml"));
    }
    public override void Save()
    {
        Save(yamlSerializer!, Path.Combine(dbPath!, "GroupDatabase.yaml"), database);
    }
    public override void Add(Group item) => Update(x => x.Id == item.Id, item);
}
