using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;

public class MaiDatabase : Database<User>, IExtension, IDatabase<User>
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
    public override void Init()
    {
        base.Init();
        database = Load<List<User>>(yamlSerializer!, Path.Combine(dbPath!, "MaiDatabase.yaml"));
    }
    public override void Save()
    {
        Save(yamlSerializer!, Path.Combine(dbPath!, "MaiDatabase.yaml"), database);
    }
}
