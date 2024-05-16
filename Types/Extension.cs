using System;

namespace TelegramBot.Types
{
    public class ExtensionCore
    {
        public static void Debug(DebugType type, string message) => Core.Debug(type, message);
    }
}
