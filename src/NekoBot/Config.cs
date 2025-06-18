using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace NekoBot;

public record BotConfig
{
    public string Token { get; init; } = "";
    public NetworkConfig Networking { get; init; } = new();
}
public class NetworkConfig
{
    public ProxyConfig Proxy { get; init; } = new();
    public int TimeoutMS { get; init; } = 2000;
}
public class ProxyConfig
{
    public bool UseProxy { get; init; } = false;
    public string Address { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
