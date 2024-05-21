using System;
using YamlDotNet.Serialization;

namespace NekoBot.Types;
public class Log
{
    public required DateTime Timestamp { get; set; }
    public required DebugType Level { get; set; }
    public required string Message { get; set; }
    public string Serialize<T>()
    {
        var serializer = new SerializerBuilder().Build();
        return serializer.Serialize(this);
    }
    public override string ToString() => $"[{Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}][{Level}] {Message}";
}
