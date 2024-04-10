using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Class;
using TelegramBot.Interfaces;

namespace TelegramBot
{
    public static class ScriptManager
    {
#nullable enable
        static List<IExtension> objs = new();
        static List<Command> commands = new();
        static Dictionary<string,IExtension> handlers = new();
        public static string ScriptPath { get => Path.Combine(Config.AppPath, "Scripts"); }
        public static void Init()
        {
            if (!Directory.Exists(ScriptPath))
                return;

            var scripts = new DirectoryInfo(ScriptPath).GetFiles()
                                           .Where(x => x.Extension is ".csx" or ".cs")
                                           .Select(x => x.FullName)
                                           .ToArray();
            foreach (var filepath in scripts)
                LoadScript(filepath);
        }
        public static void CommandHandle(InputCommand command, Update update, TUser querier, Group group)
        {
            var prefix = command.Prefix;
            if (handlers.ContainsKey(prefix))
                handlers[prefix].Handle(command, update, querier, group);
            else
                return;
        }
        public static IExtension? GetExtension(string moduleName)
        {
            var result = objs.Where(x => x.Name == moduleName).ToArray();
            if (result.Length > 0)
                return result.First();
            else
                return null;
        }
        public static T? GetExtension<T>(string moduleName) where T: class
        {
            var result = objs.Where(x => x.Name == moduleName).ToArray();
            if (result.Length > 0)
                return (T)result.First();
            else
                return null;
        }
        public static void LoadScript(string filePath)
        {
            try
            {
                var eva = CSScript.Evaluator;
                var obj = eva.LoadFile<IExtension>(filePath);
                var name = obj.Name;
                var command = obj.Commands;

                var prefixs = commands.Select(x => x.Prefix);
                foreach (var c in command)
                {
                    if (prefixs.Contains(c.Prefix))
                    {
                        Program.Debug(DebugType.Warning, $"Command \"{c.Prefix}\" is already exist,at \"{filePath}\",");
                        continue;
                    }
                    commands.Add(c);
                    handlers.Add(c.Prefix, obj);
                    objs.Add(obj);
                }
                obj.Init();
                Program.Debug(DebugType.Info, $"Loaded Script : {name} (\"{filePath}\")");
            }
            catch(Exception e)
            {
                Program.Debug(DebugType.Error, $"Loading script failure:\n{e}");
            }
        }
        public static async void UpdateCommand()
        {
            var result = commands.Select(x => 
            new BotCommand 
            { 
                Command = x.Prefix,
                Description = x.Description,                
            });
            await Program.botClient.SetMyCommandsAsync(result);
            Program.Debug(DebugType.Info,"Bot commands has been updated");
        }
        public static void Save()
        {
            foreach (var obj in objs)
                obj.Save();
        }
    }
}
