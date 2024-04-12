using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using TelegramBot;
using TelegramBot.Interfaces;
using System.Threading;
using TelegramBot.Class;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using File = System.IO.File;
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;

public partial class ScriptHandler : IExtension
{
    public string CertPath { get => Path.Combine(Config.DatabasePath,"Certs"); }
    X509Certificate2 cert = null;
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
    public string Name { get; } = "Script";
    public void Init()
    {
        if(!Directory.Exists(CertPath))
            Directory.CreateDirectory(CertPath);

        var personalCert = Path.Combine(CertPath, "LeZi9916.pem");

        if (File.Exists(Path.Combine(CertPath,"LeZi9916.pem")))
            cert = new X509Certificate2(personalCert);
    }
    public void Save()
    {

    }
    public void Destroy()
    {

    }
    public void Handle(InputCommand command, Update update, TUser querier, Group group)
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
        switch(suffix)
        {
            case "load":
                LoadScript(command, update, querier, group);
                break;
        }
    }
    void LoadScript(InputCommand command, Update update, TUser querier, Group group)
    {
        var document =  update.Message.Document;
        var file = DownloadFile();
    }
}
public partial class ScriptHandler
{
    static async Task<Message> SendMessage(string text, Update update, bool isReply = true, ParseMode? parseMode = null) => await Program.SendMessage(text, update, isReply, parseMode);
    static async void DeleteMessage(Update update) => await Program.DeleteMessage(update);
    static async Task<bool> UploadFile(string filePath, long chatId) => await Program.UploadFile(filePath, chatId);
    static async Task<bool> UploadFile(Stream stream, string fileName, long chatId) => await Program.UploadFile(stream, fileName, chatId);
    static async Task<bool> DownloadFile(string dPath, string fileId) => await Program.DownloadFile(dPath, fileId);
    static async Task<Message> EditMessage(string text, Update update, int messageId, ParseMode? parseMode = null) => await Program.EditMessage(text, update, messageId, parseMode);
}