using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using CSScripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NekoBot.Interfaces;
using NekoBot.Types;
using NekoBot;
using Version = NekoBot.Types.Version;
using Message = NekoBot.Types.Message;
using File = System.IO.File;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using System.Diagnostics;
#pragma warning disable CS4014
public partial class ScriptHelper : Extension, IExtension
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "ScriptHelper",
        Version = new Version() { Major = 1, Minor = 1, Revision = 2 },
        Type = ExtensionType.Module,
        Commands =
        [
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
        ],
        SupportUpdate =
        [
            UpdateType.Message,
            UpdateType.EditedMessage
        ]
    };
    public override void Handle(Message userMsg)
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
        var group = userMsg.Group;
        if (!querier.CheckPermission(Permission.Advanced, group))
        {
            await userMsg.Reply("Permission Denied");
            return;
        }
        if (cmd.Params.IsEmpty())
            return;
        else
        {
            string[] bannedNs = { "System.Net", "System.IO", "System.Diagnostics", "System.Runtime", "NekoBot", "AquaTools", "System.Reflection" };
            string[] bannedTypes = { "Environment", "RuntimeEnvironment", "Process" };
            var code = string.Join(" ", cmd.Params);
            var msg = await userMsg.Reply("Evaluating code...");
            Stopwatch sw = new();
            if (msg is null)
                return;
            if (CheckCode(code, bannedNs, bannedTypes) || querier.CheckPermission(Permission.Root, null))
            {
                sw.Start();
                var (result, e) = ScriptManager.EvalCode(code);
                sw.Stop();

                
                if (ScriptManager.GetExtension("CallbackQueryHandler") is ICallbackHandler callbackHandler)
                {
                    msg.InlineMarkup = Message.CreateButton(Message.DeleteButton);
                    callbackHandler.AddCallbackFunc(new CallbackHandler<CallbackMsg>(
                        cbMsg => 
                        {
                            var origin = msg;
                            if (origin?.Id != cbMsg.Origin.Id || cbMsg.Data != "getStat")
                                return false;
                            var usedTime = sw.ElapsedMilliseconds;
                            cbMsg.Client.AnswerCallbackQueryAsync(cbMsg.Id, 
                                                                  $"""
                                                                   Used Time: {usedTime}ms
                                                                   Exception: {e?.Message ?? "null"}
                                                                   """, true);
                            return false;
                        }
                    ));
                    if (e is not null)
                    {
                        msg.AddButton(InlineKeyboardButton.WithCallbackData("Stat","getStat"));
                        msg.Edit("(Complie error)");
                    }
                    else
                    {
                        msg.AddButton(InlineKeyboardButton.WithCallbackData("Stat","getStat"));
                        msg.Edit(
                            $"""
                             ```csharp
                             {StringHandle(string.IsNullOrEmpty(result) ? "(No value)" : result)}
                             ```
                             """);
                    }
                }
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
        else if (cmd.Params.Length == 0 || cmd.Params.FirstOrDefault() is "help")
        {
            GetHelpInfo((Command)userMsg.Command, userMsg);
            return;
        }

        switch (cmd.Params.FirstOrDefault())
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
        if (ext is null)
        {
            userMsg.Reply($"Error: Extension \"{extName}\" not found");
            return;
        }
        else if (extName == Info.Name || ext.Info.Type == ExtensionType.Handler)
        {
            userMsg.Reply($"Error: Cannot unload this extension");
            return;
        }
        var handler = ScriptManager.GetExtension("CallbackQueryHandler");

        if (handler is ICallbackHandler callbackHandler)
        {
            var buttons = Message.CreateButtons(
                [
                    InlineKeyboardButton.WithCallbackData("Yes","y"),
                    InlineKeyboardButton.WithCallbackData("No","n")
                ]);
            var msg = await userMsg.Reply($"Are you sure to unload \"{extName}\"?", inlineMarkup: buttons);
            callbackHandler.AddCallbackFunc(new CallbackHandler<CallbackMsg>(
                cbMsg =>
                {
                    var origin = msg;
                    var targetModule = ext;
                    if (cbMsg.From.Id != userMsg.From.Id)
                    {
                        cbMsg.Client.AnswerCallbackQueryAsync(cbMsg.Id, "Sorry\n You aren't the origin", true);
                        return false;
                    }
                    else if (origin?.Id != cbMsg.Origin.Id)
                        return false;

                    var _userMsg = cbMsg.Origin;
                    var delMarkup = Message.CreateButton(Message.DeleteButton);
                    _userMsg.InlineMarkup = null;
                    if (cbMsg.Data == "n")
                    {
                        _userMsg.InlineMarkup = delMarkup;
                        _userMsg.Edit("Operation canceled").Wait();
                        return true;
                    }
                    _userMsg.Edit($"Unloading \"{extName}\" extension...").Wait();
                    _userMsg.InlineMarkup = delMarkup;
                    try
                    {
                        ScriptManager.RemoveExtension(ext);
                        _userMsg.Edit($"Extension \"{extName}\" has removed").Wait();
                    }
                    catch (Exception e)
                    {
                        _userMsg.Edit($"Internal error:\n {e.Message}").Wait();
                    }
                    return true;
                }));
        }
        else
        {
            var msg = await userMsg.Reply($"Unloading \"{extName}\" extension...");
            if (msg is null)
                return;
            try
            {
                ScriptManager.RemoveExtension(ext);
                msg.Edit($"Extension \"{extName}\" has removed");
            }
            catch (Exception e)
            {
                msg.Edit($"Internal error:\n {e.Message}");
            }
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
        else if (ScriptManager.IsCompiling)
        {
            userMsg.Reply("Error: Script update task is running");
            return;
        }
        async Task complie(Message msg, string filePath, bool isUpdate = true)
        {
            await msg.Edit("Compiling script...(2/4)");
            var script = ScriptManager.CompileScript<IExtension>(filePath);
            if (script.Instance is not null)
            {
                var loadedScript = ScriptManager.GetExtension(script.Instance.Info.Name);
                if (loadedScript is not null)
                    await msg.Edit("Updating script...(3/4)");
                else
                    await msg.Edit("Initializing script...(3/4)");
                ScriptManager.UpdateScript(script.Instance);
                if (isUpdate)
                {
                    await msg.Edit("Overwriting script...(4/4)");
                    File.Copy(filePath, $"{Path.Combine(Config.ScriptPath, $"{script.Instance.Info.Type}/{script.Instance.Info.Name}.csx")}", true);
                }
                else
                    await msg.Edit("Clean up...(4/4)");
                await msg.Edit("Finished");
            }
            else
            {
                await msg.Edit("Error: Compile script failure\n" +
                    "```csharp\n" +
                    $"{StringHandle(script.Exception)}" +
                    "\n```", ParseMode.MarkdownV2);
                return;
            }
        }
        if (userMsg.Document is null)
        {
            var msg = await userMsg.Reply("Searching script...(1/4)");
            if (msg is null)
                return;
            var fileName = cmd.Params[1];
            var filePath = ScriptManager.GetScriptPath(fileName);

            if (string.IsNullOrEmpty(filePath))
            {
                await msg.Edit($"Error: Script file not found({fileName}.csx)");
                return;
            }
            else if (ScriptManager.GetExtension(fileName) is not null)
            {
                await msg.Edit($"Error: This extension had been loaded");
                return;
            }
            else
                await complie(msg, filePath, false);
        }
        else if (userMsg.Document is not null)
        {
            var document = userMsg.Document;
            var msg = await userMsg.Reply("Downloading document...(1/4)");
            if (msg is null)
                return;

            var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}_{document.FileName}";
            var filePath = Path.Combine(Config.TempPath, fileName);
            if (await userMsg.GetDocument(filePath))
                complie(msg, filePath);
            else
                await msg.Edit("Error: Download document failure");
        }

    }
    void GetHelpInfo(Command cmd, Message userMsg)
    {
        string helpStr = "```log\n";
        switch (cmd.Prefix)
        {
            case "script":
                helpStr += StringHandle(
                    """
                    Usage:
                    /script load   [File]   Update or add C# Script
                    /script unload [string] Unload module by name
                    /script reload          reload all scripts
                    """);
                break;
            default:
                userMsg.Reply("No helper");
                return;
        }
        helpStr += "\n```";
        userMsg.Reply(helpStr, ParseMode.MarkdownV2, true);
    }
}