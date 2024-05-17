using System.Reflection;
using Telegram.Bot.Types;
using Message = NekoBot.Types.Message;

#nullable enable
namespace NekoBot.Interfaces
{
    public interface IExtension
    {
        /// <summary>
        /// Return a assembly
        /// </summary>
        Assembly ExtAssembly { get; }
        /// <summary>
        /// Command list which extension can support
        /// </summary>
        BotCommand[] Commands { get; }
        /// <summary>
        /// Extension Name
        /// </summary>
        string Name { get; }
        void Handle(Message msg);
        void Init();
        void Save();
        void Destroy();
        MethodInfo? GetMethod(string methodName);
    }
}
