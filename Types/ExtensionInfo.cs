using System;
using System.Reflection;
using Telegram.Bot.Types;

namespace NekoBot.Types;
public class ExtensionInfo
{
    public required string Name { get; init; }
    public required Version Version { get; init; }
    public required ExtensionType Type { get; init; }
    public BotCommand[] Commands { get; init; } = Array.Empty<BotCommand>();
    public ExtensionInfo[] Dependencies { get; init; } = Array.Empty<ExtensionInfo>();
    public Assembly ExtAssembly { get => Assembly.GetExecutingAssembly(); }
}
