using System;
using System.Reflection;
using Telegram.Bot.Types;

#nullable enable
namespace TelegramBot.Types
{
    public class ExtensionCore
    {
        public Assembly ExtAssembly { get => Assembly.GetExecutingAssembly(); }
        public BotCommand[] Commands { get; } = { };
        public virtual void Handle(Message userMsg)
        {

        }
        public virtual void Init()
        {

        }
        public virtual void Save()
        {

        }
        public virtual void Destroy()
        {

        }
        public virtual MethodInfo? GetMethod(string methodName) => ExtAssembly.GetType().GetMethod(methodName);
        public static void Debug(DebugType type, string message) => Core.Debug(type, message);
    }
}
