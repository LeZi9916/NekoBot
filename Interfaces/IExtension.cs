using System.Reflection;
using Telegram.Bot.Types;
using TelegramBot.Class;

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
        Command[] Commands { get; }
        /// <summary>
        /// Extension Name
        /// </summary>
        string Name { get; }
        void Handle(InputCommand command, Update update, TUser querier, Group group);
        void Init();
        void Save();
        void Destroy();
        MethodInfo GetMethod(string methodName);
    }
}
