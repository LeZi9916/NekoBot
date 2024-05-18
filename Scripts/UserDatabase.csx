using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
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
    public override void Init()
    {
        jsonSerializer = (ISerializer)ScriptManager.GetExtension("JsonSerializer")!;
        yamlSerializer = (ISerializer)ScriptManager.GetExtension("YmalSerializer")!;
        dbPath = Config.DatabasePath;
        database = Load<List<User>>(yamlSerializer, Path.Combine(dbPath, "UserDatabase.yaml"));
    }


}
