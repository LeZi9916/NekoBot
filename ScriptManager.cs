using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBot.Class;

namespace TelegramBot
{
    internal static class ScriptManager
    {
        static List<Command> commands = new();
        public static void CommandHandle(InputCommand command, Update update, TUser querier, Group group)
        {

        }
        public static void LoadScript()
        {
            dynamic eva = CSScript.Evaluator;
            
        }
    }
}
