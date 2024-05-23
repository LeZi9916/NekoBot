using CSScriptLib;
using CZGL.SystemInfo;
using NekoBot.Exceptions;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using Message = NekoBot.Types.Message;

namespace NekoBot
{
    public class Script<T>
    {
        public T? Instance { get; init; }
        public Exception? Exception { get; init; }
    }
    public static partial class ScriptManager
    {
        public static bool IsCompiling { get; private set; } = false;

        static List<IExtension> loadedScripts = new();
        static IEvaluator evaluator = CSScript.RoslynEvaluator.Clone();
    
        static void LoadAssembly()
        {
            var libPath = Path.Combine(Config.ScriptPath, "Library");
            if (Directory.Exists(libPath))
            {
                foreach (var depend in Core.Config.Assembly)
                {
                    var path = Path.Combine(libPath, depend);
                    if (!File.Exists(path))
                    {
                        Core.Debug(DebugType.Warning, $"Assembly \"{depend}\" not found");
                        continue;
                    }    
                    try
                    {
                        evaluator.ReferenceAssembly(Assembly.LoadFrom(path));
                        Core.Debug(DebugType.Info, $"Loaded assembly: {path}");
                    }
                    catch(Exception e)
                    {
                        Core.Debug(DebugType.Error, $"Loading assembly failure: {e}");
                    }
                }
            }
        }
        public static void Init()
        {
            evaluator.Reset();
            
            if (!Directory.Exists(Config.ScriptPath))
                return;
            try
            {
                LoadAssembly();
                var scripts = GetScripts();
                var loader = new ScriptLoader(scripts.Select(x => x.Info).ToList());
                var loadOrder = loader.GetLoadOrder();
                loadOrder.Reverse();
                foreach (var name in loadOrder)
                {
                    var obj = scripts.Find(x => x.Info.Name == name);
                    AddExtension(obj);
                }
            }
            catch (Exception e)
            {
                Core.Debug(DebugType.Error, $"Loading script failure:\n{e}");
            }
        }
        public static void Save()
        {
            foreach (var obj in loadedScripts)
                obj.Save();
        }
        /// <summary>
        /// 重新加载所有Script
        /// </summary>
        /// <param name="userMsg"></param>
        public static async void Reload(Message userMsg)
        {
            IsCompiling = true;
            var msg = (await userMsg.Reply("Reloading Script..."))!;
            try
            {
                List<IExtension> newScripts = GetScripts(s => msg.Edit(s));
                var oldScripts = new List<IExtension>(loadedScripts);
                var loader = new ScriptLoader(newScripts.Select(x => x.Info).ToList());
                var loadOrder = loader.GetLoadOrder();
                loadOrder.Reverse();


                foreach (var old in oldScripts)
                    RemoveExtension(old);
                foreach (var newScript in loadOrder)
                    AddExtension(newScripts.Find(x => x.Info.Name == newScript));

                UpdateCommand();
                var scripts = string.Join("\n- ", GetLoadedScript());
                var _ = 
                    $"""
                     Scripts have been loaded:
                     - {scripts}
                     """;
                await msg.Edit(
                    $"""
                     ```python
                     {Extension.StringHandle(_)}
                     ```
                     """
                    ,ParseMode.MarkdownV2);
                GC.Collect();
            }
            catch(Exception e)
            {
                await msg.Edit("Reload script failure");
                Core.Debug(DebugType.Error, $"Reload script failure:\n{e}");
            }
            IsCompiling = false;
        }
        public static void MessageHandle(in ITelegramBotClient client,Update update)
        {
            var type = update.Type;
            var handlerName = $"{type}Handler";
            var index = loadedScripts.FindIndex(x => x.Info.Name == handlerName);
            if (index != -1)
            {
                var ext = loadedScripts[index];
                if (ext is IHandler handler)
                {
                    var foo = handler.Handle(client, loadedScripts, update);
                    if (foo is not null)
                        foo();
                }
                else
                    Core.Debug(DebugType.Warning, $"Unknown handler module \"{ext.Info.Name}\",maybe you didn't inherit and implement IHandler?");
            }
            else
                Core.Debug(DebugType.Warning, $"No handler found for handling message type \"{type}\",this message will not be handled");
        }
        /// <summary>
        /// 更新指定Script
        /// </summary>
        /// <param name="ext"></param>
        public static void UpdateScript(IExtension ext)
        {
            IsCompiling = true;
            var loadedExt = GetExtension(ext.Info.Name);
            if(loadedExt is not null)
                RemoveExtension(loadedExt);
            AddExtension(ext);            
            GC.Collect();
            IsCompiling = false;
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
            var result = loadedScripts.SelectMany(x => x.Info.Commands);
            Core.BotCommands = result.ToArray();
            await Core.GetClient().SetMyCommandsAsync(result);
            Core.Debug(DebugType.Info,"Bot commands has been updated");
        }
        
    }
    public static partial class ScriptManager
    {
        /// <summary>
        /// 根据Name获取Extension
        /// </summary>
        /// <param name="extName"></param>
        /// <returns>IExtension的实例，不存在则返回null</returns>
        public static IExtension? GetExtension(string extName) => loadedScripts.Find(x => x.Info.Name == extName);
        /// <summary>
        /// 加载并初始化该Extension
        /// </summary>
        /// <param name="extName"></param>
        public static void AddExtension(string extName) => AddExtension(GetExtension(extName));
        /// <summary>
        /// 加载并初始化该Extension
        /// </summary>
        /// <param name="ext"></param>
        public static void AddExtension(IExtension? ext)
        {
            if (ext is null) return;
            bool isConflict = false;
            foreach (var item in ext.Info.Commands)
            {
                var loadedCmds = loadedScripts.SelectMany(x => x.Info.Commands);
                if (loadedCmds.Any( x => x.Command == item.Command))
                {
                    Core.Debug(DebugType.Warning, $"Module \"{ext.Info.Name}(v{ext.Info.Version})\" command list conflicts with had been loaded module: \"{item.Command}\"");
                    isConflict = true;
                }
            }
            if (isConflict)
                return;
            loadedScripts.Add(ext);
            ext.Init();
            GC.Collect(2, GCCollectionMode.Forced);
            Core.Debug(DebugType.Info, $"Loaded script : {ext.Info.Name}");
        }
        /// <summary>
        /// 卸载该Extension
        /// </summary>
        /// <param name="extName"></param>
        public static void RemoveExtension(string extName) => RemoveExtension(GetExtension(extName));
        /// <summary>
        /// 卸载该Extension
        /// </summary>
        /// <param name="ext"></param>
        public static void RemoveExtension(IExtension? ext)
        {
            if (ext is null || !loadedScripts.Contains(ext)) return;
            var name = ext.Info.Name;
            loadedScripts.Remove(ext);
            
            if(ext is IDestroyable _ext)
                _ext.Destroy();
            GC.Collect();
            Core.Debug(DebugType.Info, $"Unloaded script : {name}");
        }

        /// <summary>
        /// 获取已加载Script的Name
        /// </summary>
        /// <returns></returns>
        public static string[] GetLoadedScript() => loadedScripts.Select(x => $"{x.Info.Name}(v{x.Info.Version})").ToArray();
        static List<IExtension> GetScripts() => GetScripts(s => { });
        public static FileInfo[] GetFiles(string path)
        {
            List<FileInfo> files = new();
            Stack<string> dirs = new();
            dirs.Push(path);

            while(dirs.Count > 0)
            {
                var dirPath = dirs.Pop();
                files.AddRange(Directory.GetFiles(dirPath).Select(x => new FileInfo(x)));

                foreach (var dir in Directory.GetDirectories(dirPath))
                    dirs.Push(dir);
            }
            return files.ToArray();
        }
        public static string? GetScriptPath(string extName)
        {
            return GetFiles(Config.ScriptPath).Where(x => x.Extension is ".csx" or ".cs" && x.Name == $"{extName}.csx")
                                       .Select(x => x.FullName)
                                       .First();
        }
        static List<IExtension> GetScripts(Action<string> step)
        {
            var scriptPaths = GetFiles(Config.ScriptPath).Where(x => x.Extension is ".csx" or ".cs")
                                                  .Select(x => x.FullName)
                                                  .ToArray();
            List<IExtension> uninitObjs = new();
            foreach (var path in scriptPaths)
            {
                try
                {
                    step($"Compiling \"{new FileInfo(path).Name}\"...");
                    var obj = evaluator.LoadFile<IExtension>(path);
                    var info = obj.Info;
                    var conflictObj = uninitObjs.Find(x => x.Info.Name == info.Name);
                    if (conflictObj is not null)
                    {
                        if (conflictObj.Info.Version < info.Version)
                        {
                            uninitObjs.Remove(conflictObj);
                            Core.Debug(DebugType.Info, $"Conflicting scripts, removing: {conflictObj.Info.Name}(v{conflictObj.Info.Version})");
                        }
                        else
                            continue;
                    }
                    uninitObjs.Add(obj);
                    Core.Debug(DebugType.Info, $"Compiled script: {info.Name}(v{info.Version})");
                }
                catch (Exception e)
                {
                    var name = new FileInfo(path).Name;
                    Core.Debug(DebugType.Error, $"Compiling script failure ({name}):\n{e}");
                    step($"Compiling script failure ({name})");

                }
            }
            return uninitObjs;
        }
        /// <summary>
        /// 获取主Assembly的版本号
        /// </summary>
        /// <returns></returns>
        public static System.Version? GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    }
    public class ScriptLoader
    {
        Dictionary<string, ExtensionInfo> scriptInfos;
        HashSet<string> visited = new();
        HashSet<string> visiting = new();
        Stack<string> sortedScripts = new();

        public ScriptLoader(List<ExtensionInfo> scriptsList)
        {
            scriptInfos = scriptsList.ToDictionary(x => x.Name);
        }
        public List<string> GetLoadOrder()
        {
            foreach (var scriptInfo in scriptInfos.Values)
            {
                if (!visited.Contains(scriptInfo.Name))
                {
                    if (!Sort(scriptInfo))
                        throw new InvalidOperationException("Circular dependency detected");
                }
            }
            return sortedScripts.ToList();
        }
        bool Sort(ExtensionInfo info)
        {
            if (visited.Contains(info.Name))
                return true;

            if (visiting.Contains(info.Name))
                return false;

            visiting.Add(info.Name);

            foreach (var depend in info.Dependencies)
            {
                if (!scriptInfos.ContainsKey(depend.Name))
                    throw new DependNotFoundException($"Script \"{info.Name}\" depends on \"{depend.Name}\",but \"{depend.Name}\" is not found");
                if (!Sort(scriptInfos[depend.Name]))
                    return false;
            }

            visiting.Remove(info.Name);
            visited.Add(info.Name);
            sortedScripts.Push(info.Name);

            return true;
        }
    }
}
