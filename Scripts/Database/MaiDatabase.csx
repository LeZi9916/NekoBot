//using NekoBot.Interfaces;
//using NekoBot.Types;
//using System.Collections.Generic;
//using System.IO;
//using Version = NekoBot.Types.Version;

//public class MaiDatabase : Database<MaiAccount>, IExtension, IDatabase<MaiAccount>, IDestroyable
//{
//    public new ExtensionInfo Info { get; } = new ExtensionInfo()
//    {
//        Name = "MaiDatabase",
//        Version = new Version() { Major = 1, Minor = 0 },
//        Type = ExtensionType.Database,
//        Dependencies = 
//        [
//            new ExtensionInfo()
//            {
//                Name = "JsonSerializer",
//                Version = new Version() { Major = 1, Minor = 0 },
//                Type = ExtensionType.Serializer
//            },
//            new ExtensionInfo()
//            {
//                Name = "YamlSerializer",
//                Version = new Version() { Major = 1, Minor = 0 },
//                Type = ExtensionType.Serializer
//            }
//        ]
//    };
//    public override void Init()
//    {
//        base.Init();
//        dbPath = Path.Combine(Config.DatabasePath, "MaiDatabase.yaml");
//        _database = Load<List<MaiAccount>>(yamlSerializer!,dbPath);
//        AutoSave();
//    }
//    public override void Destroy()
//    {
//        base.Destroy();
//        Save();
//    }
//}
