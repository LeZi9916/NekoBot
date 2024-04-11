using CSScriptLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Class;
using TelegramBot.Interfaces;
using File = System.IO.File;

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
                Load(filepath);
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
        public static void Load(string filePath)
        {
            try
            {
                var filename = GetRandomStr().Replace("/","");
                var dllPath = $"{Config.TempPath}/{filename}.dll";
                var eva = CSScript.Evaluator;
                eva.CompileAssemblyFromFile(filePath,dllPath);
                var dllRef = eva.ReferenceAssembly(dllPath);
                var obj = dllRef.LoadFile<IExtension>(filePath);
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
                    
                }
                objs.Add(obj);
                obj.Init();
                Program.Debug(DebugType.Info, $"Loaded Script : {name} (\"{filePath}\")");
            }
            catch(Exception e)
            {
                Program.Debug(DebugType.Error, $"Loading script failure ({filePath}):\n{e}");
            }
        }
        public static T LoadScript<T>(string filePath) where T:class => CSScript.Evaluator.LoadFile<T>(filePath);
        public static async void UpdateCommand()
        {
            var result = commands.Select(x => 
            new BotCommand 
            { 
                Command = x.Prefix,
                Description = x.Description,                
            });
            Program.BotCommands = result.ToArray();
            await Program.botClient.SetMyCommandsAsync(result);
            Program.Debug(DebugType.Info,"Bot commands has been updated");
        }
        public static string[] GetLoadedScript() => objs.Select(x => x.Name).ToArray();
        public static void Save()
        {
            foreach (var obj in objs)
                obj.Save();
        }
        public static Version? GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;
        public static async void Reload(Update update)
        {
            await Task.Run(() =>
            {
                List<IExtension> _objs = new();
                List<Command> _commands = new();
                Dictionary<string, IExtension> _handlers = new();
                var scripts = new DirectoryInfo(ScriptPath).GetFiles()
                                               .Where(x => x.Extension is ".csx" or ".cs")
                                               .Select(x => x.FullName)
                                               .ToArray();
                string name = "";
                try
                {                    
                    foreach (var filePath in scripts)
                    {
                        var obj = LoadScript<IExtension>(filePath);
                        name = obj.Name;
                        var command = obj.Commands;
                        var prefixs = commands.Select(x => x.Prefix);

                        foreach (var c in command)
                        {
                            if (prefixs.Contains(c.Prefix))
                            {
                                Program.Debug(DebugType.Warning, $"Command \"{c.Prefix}\" is already exist,at \"{filePath}\",");
                                continue;
                            }
                            _commands.Add(c);
                            _handlers.Add(c.Prefix, obj);
                            _objs.Add(obj);
                        }
                    }
                    objs.Clear();
                    commands.Clear();
                    handlers.Clear();
                    objs = _objs;
                    commands = _commands;
                    handlers = _handlers;

                    objs.ForEach(x => x.Init());
                    UpdateCommand();
                    Program.SendMessage(
                        "以下Script已加载:\n" +
                        $"-{string.Join("\n-",objs.Select(x=>x.Name))}", update);
                }
                catch(Exception e)
                {
                    Program.SendMessage(
                        $"重新加载\"{name}\"时发生错误:\n" +
                        "```csharp\n" +
                        Program.StringHandle(e.ToString()) +
                        "\n```", 
                        update,true,ParseMode.MarkdownV2);
                }
            });
        }
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    }
}
