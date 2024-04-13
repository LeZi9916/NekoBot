using CSScriptLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Class;
using TelegramBot.Interfaces;
using File = System.IO.File;

#nullable enable
namespace TelegramBot
{
    public class Script<T>
    {
        public T? Instance { get; init; }
        public Exception? Exception { get; init; }
    }
    public static class ScriptManager
    {

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
        public static void Load(string filePath)
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

                }
                objs.Add(obj);
                obj.Init();
                Program.Debug(DebugType.Info, $"Loaded Script : {name} (\"{filePath}\")");
            }
            catch (Exception e)
            {
                Program.Debug(DebugType.Error, $"Loading script failure ({filePath}):\n{e}");
            }
        }
        public static void Save()
        {
            foreach (var obj in objs)
                obj.Save();
        }
        public static async void Reload(Update update)
        {
            var msg = await Program.SendMessage("正在尝试重新加载Script...", update);
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
                    var obj = CompileScript<IExtension>(filePath).Instance;
                    name = obj.Name;
                    var command = obj.Commands;
                    var prefixs = _commands.Select(x => x.Prefix);

                    foreach (var c in command)
                    {
                        if (prefixs.Contains(c.Prefix))
                        {
                            Program.Debug(DebugType.Warning, $"Command \"{c.Prefix}\" is already exist,at \"{filePath}\",");
                            continue;
                        }
                        _commands.Add(c);
                        _handlers.Add(c.Prefix, obj);

                    }
                    _objs.Add(obj);
                }
                objs.Clear();
                commands.Clear();
                handlers.Clear();
                objs = _objs;
                commands = _commands;
                handlers = _handlers;

                objs.ForEach(x => x.Init());
                UpdateCommand();
                await Program.EditMessage(
                    "以下Script已加载:\n" +
                    $"-{string.Join("\n-", objs.Select(x => x.Name))}", update, msg.MessageId);
            }
            catch (Exception e)
            {
                await Program.EditMessage(
                    $"重新加载\"{name}\"时发生错误:\n" +
                    "```csharp\n" +
                    Program.StringHandle(e.ToString()) +
                    "\n```",
                    update, msg.MessageId, ParseMode.MarkdownV2);
            }
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
        public static void UpdateScript(IExtension ext)
        {
            var loadedExt = GetExtension(ext.Name);
            if(loadedExt is not null)
            {
                loadedExt.Destroy();
                objs.Remove(loadedExt);
            }
            objs.Add(ext);
            ext.Init();
        }
        public static string? EvalCode(string code)
        {
            try
            {
                var eval = CSScript.RoslynEvaluator.Clone().Reset(false);
                return ((object)eval.Eval(code))?.ToString();
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }
        public static Script<T> CompileScript<T>(string filePath) where T:class
        {
            try
            {
                return new Script<T>()
                {
                    Instance = CSScript.Evaluator.LoadFile<T>(filePath),
                    Exception = null
                };
                
            }
            catch(Exception e)
            {
                return new Script<T>()
                {
                    Instance = null,
                    Exception = e
                };
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
            Program.BotCommands = result.ToArray();
            await Program.botClient.SetMyCommandsAsync(result);
            Program.Debug(DebugType.Info,"Bot commands has been updated");
        }
        public static string[] GetLoadedScript() => objs.Select(x => x.Name).ToArray();
        public static Version? GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    }
}
