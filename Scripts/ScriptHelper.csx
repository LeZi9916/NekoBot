using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TelegramBot;
using TelegramBot.Interfaces;
using TelegramBot.Class;
using System.Linq;
using File = System.IO.File;
using System;
using CSScripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Reflection;
#pragma warning disable CS4014
public partial class ScriptHelper : IExtension
{
    public Assembly ExtAssembly { get => Assembly.GetExecutingAssembly(); }
    public Command[] Commands { get; } =
    {
        new Command()
        {
            Prefix = "script",
            Description = "Script管理"
        },
        new Command()
        {
            Prefix = "eval",
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
    public void Handle(InputCommand command, Update update, TUser querier, Group group)
    {
        switch (command.Prefix)
        {
            case "script":
                ScriptHandle(command, update, querier, group);
                break;
            case "eval":
                EvalHandle(command, update, querier, group);
                break;
        }
    }
    public void EvalHandle(InputCommand command, Update update, TUser querier, Group group)
    {
        var message = update.Message;
        if (!querier.CheckPermission(Permission.Advanced, group))
        {
            SendMessage("Permission Denied", update, true);
            return;
        }
        if (command.Content.IsEmpty())
            return;
        else
        {
            string[] bannedNs = { "System.Net", "System.IO", "System.Diagnostics", "System.Runtime", "TelegramBot", "AquaTools" };
            string[] bannedTypes = { "Environment", "RuntimeEnvironment", "Process" };
            var code = string.Join(" ", command.Content);
            var msg = SendMessage("Compiling code...", update).Result;
            if (CheckCode(code, bannedNs, bannedTypes) || querier.CheckPermission(Permission.Root, null))
            {
                var result = ScriptManager.EvalCode(code) ?? "null";
                var _result = string.IsNullOrEmpty(result) ? "empty" : result;
                EditMessage("```csharp\n" +
                        $"{Program.StringHandle(result)}\n" +
                        $"```", update,msg.MessageId, ParseMode.MarkdownV2);
            }
            else
                SendMessage("Unsupport operate", update, true, ParseMode.MarkdownV2);

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
    public void ScriptHandle(InputCommand command, Update update, TUser querier, Group group)
    {
        var message = update.Message;
        if (!querier.CheckPermission(Permission.Root, group))
        {
            SendMessage("Permission Denied", update, true);
            return;
        }
        else if (command.Content.Length == 0 || command.Content.Length > 0 && command.Content[0] is "help")
        {
            //GetHelpInfo(command, update, querier, group);
            return;
        }

        var suffix = command.Content[0];
        command.Content = command.Content.Skip(1).ToArray();
        switch (suffix)
        {
            case "load":
                LoadScript(command, update, querier, group);
                break;
        }
    }
    async void LoadScript(InputCommand command, Update update, TUser querier, Group group)
    {
        var document = update.Message.Document;

        if (document is null)
        {
            SendMessage("Please upload a C# Script file", update);
            return;
        }
        else if(ScriptManager.IsCompiling)
        {
            SendMessage("Script update task is running", update);
            return;
        }
        var msg = await SendMessage("Downloading document...(1/4)", update);
        var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}_{document.FileName}";
        var filePath = Path.Combine(Config.TempPath, fileName);

        if (await DownloadFile(filePath, document.FileId))
        {
            EditMessage("Compiling script...(2/4)", update, msg.MessageId);
            var script = ScriptManager.CompileScript<IExtension>(filePath);
            if (script.Instance is not null)
            {
                var loadedScript = ScriptManager.GetExtension(script.Instance.Name);
                if (loadedScript is not null)
                    EditMessage("Updating script...(3/4)", update, msg.MessageId);
                else
                    EditMessage("Initializing script...(3/4)", update, msg.MessageId);
                ScriptManager.UpdateScript(script.Instance);
                EditMessage("Overwriting script...(4/4)", update, msg.MessageId);
                File.Copy(filePath, $"{Path.Combine(ScriptManager.ScriptPath, $"{script.Instance.Name}.csx")}", true);
                EditMessage("Finished", update, msg.MessageId);
            }
            else
            {
                EditMessage("Error: Compile script failure\n" +
                    "```csharp\n" +
                    $"{Program.StringHandle(script.Exception.ToString())}" +
                    "\n```", update, msg.MessageId, ParseMode.MarkdownV2);
                return;
            }
        }
        else
            EditMessage("Error: Download document failure", update, msg.MessageId);

    }
}
public partial class ScriptHelper
{
    static async Task<Message> SendMessage(string text, Update update, bool isReply = true, ParseMode? parseMode = null) => await Program.SendMessage(text, update, isReply, parseMode);
    static async void DeleteMessage(Update update) => await Program.DeleteMessage(update);
    static async Task<bool> UploadFile(string filePath, long chatId) => await Program.UploadFile(filePath, chatId);
    static async Task<bool> UploadFile(Stream stream, string fileName, long chatId) => await Program.UploadFile(stream, fileName, chatId);
    static async Task<bool> DownloadFile(string dPath, string fileId) => await Program.DownloadFile(dPath, fileId);
    static async Task<Message> EditMessage(string text, Update update, int messageId, ParseMode? parseMode = null) => await Program.EditMessage(text, update, messageId, parseMode);
}