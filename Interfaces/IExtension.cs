using System.Reflection;
using Telegram.Bot.Types;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;

namespace TelegramBot.Interfaces
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
        MethodInfo GetMethod(string methodName);
    }
}
