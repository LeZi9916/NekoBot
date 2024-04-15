using CSScripting;
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
    public static partial class ScriptManager
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

                AddExtension(obj);                
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
        /// <summary>
        /// 重新加载所有Script
        /// </summary>
        /// <param name="update"></param>
        public static async void Reload(Update update)
        {
            var msg = await Program.SendMessage("Reloading Script...", update);
            try
            {                
                List<IExtension> newObjs = new();
                var scripts = new DirectoryInfo(ScriptPath).GetFiles()
                                               .Where(x => x.Extension is ".csx" or ".cs")
                                               .Select(x => x.FullName);
                foreach (var filePath in scripts)
                {
                    var fileName = new FileInfo(filePath).Name;
                    await Program.EditMessage($"Compiling \"{fileName}\"...", update, msg.MessageId);
                    try
                    {
                        var obj = CompileScript<IExtension>(filePath).Instance;
                        newObjs.Add(obj);
                    }
                    catch (Exception e)
                    {
                        await Program.EditMessage(
                            $"Loading \"{fileName}\" failure:\n" +
                            "```csharp\n" +
                            Program.StringHandle(e.ToString()) +
                            "\n```",
                            update, msg.MessageId, ParseMode.MarkdownV2);
                    }
                }

                var _objs = objs.ToArray();
                var needUpdate = _objs.Where(x => newObjs.Any(y => y.Name == x.Name))
                                      .Select(x => x.Name);

                foreach (var oldExt in _objs.Where(x => needUpdate.Any(y => x.Name == y)))
                    RemoveExtension(oldExt);

                foreach (var ext in newObjs.Where(x => needUpdate.Any(y => x.Name == y)))
                    AddExtension(ext);

                UpdateCommand();
                await Program.EditMessage(
                    "Scripts have been loaded:\n" +
                    $"-{string.Join("\n-", objs.Select(x => x.Name))}", update, msg.MessageId);
                GC.Collect();
            }
            catch(Exception e)
            {
                await Program.EditMessage("Reload script failure", update, msg.MessageId);
                Program.Debug(DebugType.Error, $"Reload script failure:\n{e}");
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
        /// <summary>
        /// 更新指定Script
        /// </summary>
        /// <param name="ext"></param>
        public static void UpdateScript(IExtension ext)
        {
            var loadedExt = GetExtension(ext.Name);
            if(loadedExt is not null)
                RemoveExtension(loadedExt);
            AddExtension(ext);            
            GC.Collect();
        }
        /// <summary>
        /// 执行传入的C#代码，并返回String
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 编译指定的C#文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns>第一个类的实例</returns>
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
        /// <summary>
        /// 更新Bot的Command列表
        /// </summary>
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
        
    }
    public static partial class ScriptManager
    {
        /// <summary>
        /// 根据Name获取Extension
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns>IExtension的实例，不存在则返回null</returns>
        public static IExtension? GetExtension(string extName)
        {
            var result = objs.Where(x => x.Name == extName).ToArray();
            if (result.Length > 0)
                return result.First();
            else
                return null;
        }
        /// <summary>
        /// 加载并初始化该Extension
        /// </summary>
        /// <param name="extName"></param>
        static void AddExtension(string extName) => AddExtension(GetExtension(extName));
        /// <summary>
        /// 加载并初始化该Extension
        /// </summary>
        /// <param name="ext"></param>
        static void AddExtension(IExtension? ext)
        {
            if (ext is null) return;

            foreach (var item in ext.Commands)
            {
                if (handlers.ContainsKey(item.Prefix))
                    continue;
                handlers.Add(item.Prefix, ext);
                commands.Add(item);
            }
            objs.Add(ext);
            ext.Init();
            Program.Debug(DebugType.Info, $"Loaded script : {ext.Name}");
        }
        /// <summary>
        /// 卸载该Extension
        /// </summary>
        /// <param name="ext"></param>
        static void RemoveExtension(string extName) => RemoveExtension(GetExtension(extName));
        /// <summary>
        /// 卸载该Extension
        /// </summary>
        /// <param name="ext"></param>
        static void RemoveExtension(IExtension? ext)
        {
            if (ext is null || !objs.Contains(ext)) return;

            var oldKeys = handlers.Where(x => x.Value == ext).Select(x => x.Key);
            foreach (var key in oldKeys)
                handlers.Remove(key);
            foreach(var cmd in ext.Commands)
                commands.Remove(cmd);
            objs.Remove(ext);
            ext.Destroy();
            Program.Debug(DebugType.Info, $"Unloaded script : {ext.Name}");
        }

        /// <summary>
        /// 获取已加载Script的Name
        /// </summary>
        /// <returns></returns>
        public static string[] GetLoadedScript() => objs.Select(x => x.Name).ToArray();
        /// <summary>
        /// 获取主Assembly的版本号
        /// </summary>
        /// <returns></returns>
        public static Version? GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    }
}
