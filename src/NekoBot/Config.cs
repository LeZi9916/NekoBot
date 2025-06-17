using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace NekoBot;
public record BotConfig
{
    public string Token { get; init; } = "";
    public Proxy Proxy { get; init; } = new();
}
public class Proxy
{
    public bool UseProxy { get; init; } = false;
    public string? Address { get; init; } = null;
}
