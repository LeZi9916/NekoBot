using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using NekoBot.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Action = System.Action;
using Message = NekoBot.Types.Message;
using Version = NekoBot.Types.Version;
using User = NekoBot.Types.User;

#pragma warning disable CS4014
public class CallbackQueryHandler : Destroyable, IExtension, IHandler, ICallbackHandler, IDestroyable
{
    Dictionary<IExtension, List<Action<CallbackMsg>>> submiters = new();
    public event Action<CallbackMsg>? OnCallback;

    IDatabase<User>? userDatabase;
    IDatabase<Group>? groupDatabase;
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "CallbackQueryHandler",
        Version = new Version() { Major = 1, Minor = 0, Revision = 0 },
        Type = ExtensionType.Handler,
        SupportUpdate =
        [
            UpdateType.CallbackQuery
        ],
        Dependencies = 
        [
            new ExtensionInfo()
            {
                Name = "UserDatabase",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Database
            },
            new ExtensionInfo()
            {
                Name = "GroupDatabase",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Database
            }
        ]
    };
    public override void Init()
    {
        var userDB = (IDatabase<User>?)ScriptManager.GetExtension("UserDatabase");
        var groupDB = (IDatabase<Group>?)ScriptManager.GetExtension("GroupDatabase");
        if (userDB is null || groupDB is null)
            throw new DatabaseNotFoundException("This script depends on the database to initialize");
        userDatabase = userDB;
        groupDatabase = groupDB;
    }
    CallbackMsg? PreHandle(in ITelegramBotClient client, Update update)
    {
        var query = update.CallbackQuery!;
        UserDiscover(query.From);
        var userMsg = Message.Parse(client, query.Message);
        if (userMsg is null)
            return null;

        var from = userDatabase!.Find(x => x.Id == userMsg.From.Id);
        userMsg.From = from!;
        userMsg.Group = groupDatabase!.Find(x => x.Id == userMsg.Chat.Id);

        if (userMsg.ReplyTo is not null)
        {
            var replyTo = userDatabase!.Find(x => x.Id == userMsg.ReplyTo.From.Id);
            userMsg.ReplyTo!.From = replyTo!;
        }
        return new CallbackMsg()
        {
            From = userDatabase!.Find(x => x.Id == query.From.Id)!,
            Origin = userMsg,
            Data = query.Data,
        };
    }
    public Action? Handle(in ITelegramBotClient client, in List<IExtension> extensions, Update update)
    {
        if (update.CallbackQuery is null)
            return default;
        var msg = PreHandle(client, update);
        if (msg is null)
            return default;
        else if (msg.Data == "delMsg")
            msg.Origin.Delete();
        else if(OnCallback is not null)
            OnCallback(msg);
        return default;
    }
    void UserDiscover(in Telegram.Bot.Types.User? newUser)
    {
        if (newUser is null)
            return;

        var id = newUser.Id;
        var record = userDatabase!.Find(x => x.Id == id);
        if(record is null)
        {
            userDatabase.Add(newUser!);
            Debug(DebugType.Info, $"Find New User:\n" +
                    $"Name: {newUser.FirstName} {newUser.LastName}\n" +
                    $"isBot: {newUser.IsBot}\n" +
                    $"Username: {newUser.Username}\n" +
                    $"isPremium: {newUser.IsPremium}");
        }
        else if(record.Update(newUser!))
        {
            userDatabase.Update(x => x.Id == id, record);
            Debug(DebugType.Info, $"User info had been updated:\n" +
            $"Name: {newUser.FirstName} {newUser.LastName}\n" +
            $"isBot: {newUser.IsBot}\n" +
            $"Username: {newUser.Username}\n" +
            $"isPremium: {newUser.IsPremium}");
        }
    }
    public override void Destroy()
    {
        base.Destroy();
        OnCallback = null;
    }
    public void AddCallbackFunc(IExtension submiter, Action<CallbackMsg> func)
    {
        if (!submiters.ContainsKey(submiter))
            submiters.Add(submiter, new());
        else if (submiters[submiter].Contains(func))
            return;
        submiters[submiter].Add(func);
        OnCallback += func;
    }
    public void RemoveAllFunc(IExtension submiter)
    {
        if (!submiters.ContainsKey(submiter))
            return;
        foreach(var func in submiters[submiter])
            OnCallback -= func;
        submiters.Remove(submiter);
    }
    public void RemoveCallbackFunc(IExtension submiter, Action<CallbackMsg> func)
    {
        if (!submiters.ContainsKey(submiter))
            return;
        submiters[submiter].Remove(func);
        OnCallback -= func;
    }
}
