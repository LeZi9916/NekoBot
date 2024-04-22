using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TelegramBot;
using TelegramBot.Interfaces;
using System.Linq;
using File = System.IO.File;
using System;
using CSScripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Reflection;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;
#pragma warning disable CS4014
public partial class ScriptHelper : ScriptCommon, IExtension
{
    public Assembly ExtAssembly { get => Assembly.GetExecutingAssembly(); }
    public BotCommand[] Commands { get; } =
    {
        new BotCommand()
        {
            Command = "script",
           Description = "Script管理"
        },
        new BotCommand()
        {
           Command = "reload",
            Description = "重新加载Script"
        },
        new BotCommand()
        {
           Command = "eval",
           Description = "CS 解析器"
        }
    };
    public string Name { get; } = "ScriptHelper";
    public void Init()
    {

    }
    public void Save()
    {

    }
    public void Destroy()
    {

    }
    public MethodInfo GetMethod(string methodName) => ExtAssembly.GetType().GetMethod(methodName);
    public void Handle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        switch (cmd.Prefix)
        {
            case "script":
                ScriptHandle(userMsg);
                break;
            case "eval":
                EvalHandle(userMsg);
                break;
            case "reload":
                ReloadScript(userMsg);
                break;
        }
    }
    public async void EvalHandle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var group = userMsg.GetGroup();
        if (!querier.CheckPermission(Permission.Advanced, group))
        {
            await userMsg.Reply("Permission Denied");
            return;
        }
        if (cmd.Params.IsEmpty())
            return;
        else
        {
            string[] bannedNs = { "System.Net", "System.IO", "System.Diagnostics", "System.Runtime", "TelegramBot", "AquaTools" };
            string[] bannedTypes = { "Environment", "RuntimeEnvironment", "Process" };
            var code = string.Join(" ", cmd.Params);
            var msg = await userMsg.Reply("Compiling code...");
            if (CheckCode(code, bannedNs, bannedTypes) || querier.CheckPermission(Permission.Root, null))
            {
                var result = ScriptManager.EvalCode(code) ?? "null";
                var _result = string.IsNullOrEmpty(result) ? "empty" : result;
                msg.Edit("```csharp\n" +
                        $"{Program.StringHandle(result)}\n" +
                        $"```",ParseMode.MarkdownV2);
            }
            else
                userMsg.Reply("Unsupport operate", ParseMode.MarkdownV2);

        }
    }
    bool CheckCode(string code, string[] banNamespaces, string[] banTypes)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var descendantNodes = root.DescendantNodes();

        var hasBannedNs = descendantNodes.OfType<UsingDirectiveSyntax>()
                                         .Any(x => banNamespaces.Contains(x.Name.ToString()));
        hasBannedNs = hasBannedNs || descendantNodes.OfType<QualifiedNameSyntax>()
                                                    .Any(x => banNamespaces.Contains(x.ToString()));
        var hasBannedType = root.DescendantNodes()
                                .OfType<IdentifierNameSyntax>()
                                .Any(x => banTypes.Contains(x.Identifier.ValueText));

        return !(hasBannedNs || hasBannedType);
    }
    public void ScriptHandle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;

        if (!querier.CheckPermission(Permission.Root))
        {
            userMsg.Reply("Permission Denied");
            return;
        }
        else if (cmd.Params.Length == 0 || cmd.Params.First() is "help")
        {
            //GetHelpInfo(command, update, querier, group);
            return;
        }

        switch (cmd.Params.First())
        {
            case "load":
                LoadScript(userMsg);
                break;
            case "unload":
                UnloadScript(userMsg);
                break;
            case "reload":
                ReloadScript(userMsg);
                break;
        }
    }
    void ReloadScript(Message userMsg)
    {
        var querier = userMsg.From;
        if (!querier.CheckPermission(Permission.Root))
        {
            userMsg.Reply("Permission Denied");
            return;
        }
        else if (ScriptManager.IsCompiling)
        {
            userMsg.Reply("Script update task is running");
            return;
        }
        ScriptManager.Reload(userMsg);
    }
    async void UnloadScript(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;

        if (cmd.Params.Length != 2)
        {
            userMsg.Reply("Error: Invaild extension name");
            return;
        }
        var extName = cmd.Params[1];
        var ext = ScriptManager.GetExtension(extName);
        if(ext is null)
        {
            userMsg.Reply($"Error: Extension \"{extName}\" not found");
            return;
        }
        else if(extName == Name)
        {
            userMsg.Reply($"Error: Cannot unload this extension");
            return;
        }

        var msg = await userMsg.Reply($"Unloading \"{extName}\" extension...");
        try
        {
            ScriptManager.RemoveExtension(ext);
            msg.Edit($"Extension \"{extName}\" has removed");
        }
        catch(Exception e)
        {
            msg.Edit($"Internal error:\n {e.Message}");
        }

    }
    async void LoadScript(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;

        if (userMsg.Document is null && cmd.Params.Length != 2)
        {
            userMsg.Reply("Error: Please upload a C# Script file");
            return;
        }
        else if(ScriptManager.IsCompiling)
        {
            userMsg.Reply("Error: Script update task is running");
            return;
        }
        async Task complie(Message msg,string filePath,bool isUpdate = true)
        {
            await msg.Edit("Compiling script...(2/4)");
            var script = ScriptManager.CompileScript<IExtension>(filePath);
            if (script.Instance is not null)
            {
                var loadedScript = ScriptManager.GetExtension(script.Instance.Name);
                if (loadedScript is not null)
                    await msg.Edit("Updating script...(3/4)");
                else
                    await msg.Edit("Initializing script...(3/4)");
                ScriptManager.UpdateScript(script.Instance);
                if(isUpdate)
                {
                    await msg.Edit("Overwriting script...(4/4)");
                    File.Copy(filePath, $"{Path.Combine(ScriptManager.ScriptPath, $"{script.Instance.Name}.csx")}", true);
                }
                else
                    await msg.Edit("Clean up...(4/4)");
                await msg.Edit("Finished");
            }
            else
            {
                await msg.Edit("Error: Compile script failure\n" +
                    "```csharp\n" +
                    $"{Program.StringHandle(script.Exception.ToString())}" +
                    "\n```",ParseMode.MarkdownV2);
                return;
            }
        }
        if (userMsg.Document is null)
        {
            var msg = await userMsg.Reply("Searching script...(1/4)");
            var fileName = cmd.Params[1];
            var filePath = Path.Combine(ScriptManager.ScriptPath, $"{fileName}.csx");
            
            if(!File.Exists(filePath))
            {
                await msg.Edit($"Error: Script file not found({fileName}.csx)");
                return;
            }
            else if(ScriptManager.GetExtension(fileName) is not null)
            {
                await msg.Edit($"Error: This extension had been loaded");
                return;
            }
            else
                await complie(msg, filePath,false);
        }
        else if(userMsg.Document is not null)
        {
            var document = userMsg.Document;
            var msg = await userMsg.Reply("Downloading document...(1/4)");
            var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}_{document.FileName}";
            var filePath = Path.Combine(Config.TempPath, fileName);
            if (await userMsg.GetDocument(filePath))
                complie(msg, filePath);
            else
                await msg.Edit("Error: Download document failure");
        }

    }
}