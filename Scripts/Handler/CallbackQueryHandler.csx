using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using NekoBot.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
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
    List<WeakReference<CallbackHandler<CallbackMsg>>> submiters = new();

    IDatabase<User> userDatabase
    {
        get
        {
            var dbManager = ScriptManager.GetExtension("MongoDBManager") as IDBManager;
            var newDB = dbManager?.GetCollection<User>("User");
            if (newDB is not null and IDatabase<User> db &&
               db != _userDatabase)
            {
                _userDatabase = db;
            }
            return _userDatabase ?? throw new DatabaseNotFoundException("This script depends on the database");
        }
    }
    IDatabase<Group> groupDatabase
    {
        get
        {
            var dbManager = ScriptManager.GetExtension("MongoDBManager") as IDBManager;
            var newDB = dbManager?.GetCollection<Group>("Group");
            if (newDB is not null and IDatabase<Group> db &&
                db != _groupDatabase)
            {
                _groupDatabase = db;
            }
            return _groupDatabase ?? throw new DatabaseNotFoundException("This script depends on the database");
        }
    }
    IDatabase<User>? _userDatabase;
    IDatabase<Group>? _groupDatabase;
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "CallbackQueryHandler",
        Version = new Version() { Major = 1, Minor = 0, Revision = 1 },
        Type = ExtensionType.Handler,
        SupportUpdate =
        [
            UpdateType.CallbackQuery
        ],
        Dependencies = 
        [
            new ExtensionInfo()
            {
                Name = "MongoDBManager",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Database
            }
        ]
    };
    public override void Init()
    {
        var dbManager = ((ScriptManager.GetExtension("MongoDBManager") ?? throw new DatabaseNotFoundException("This script depends on the database to initialize"))
                        as IDBManager)!;
        _userDatabase = dbManager.GetCollection<User>("User");
        _groupDatabase = dbManager.GetCollection<Group>("Group");

        _userDatabase.OnDestroy += () => _userDatabase = null;
        _groupDatabase.OnDestroy += () => _groupDatabase = null;
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
            Id = query.Id,
            From = userDatabase!.Find(x => x.Id == query.From.Id)!,
            Origin = userMsg,
            Data = query.Data,
            Client = client
        };
    }
    public Action? Handle(in ITelegramBotClient client, in List<IExtension> extensions, Update update)
    {
        if (update.CallbackQuery is null)
            return default;
        var callbackQuery = update.CallbackQuery;
        var msg = PreHandle(client, update);
        if (msg is null)
            return default;
        else if (msg.Data == "delMsg")
        {
            try
            {
                msg.Origin.Delete();
            }
            catch
            {
                client.AnswerCallbackQueryAsync(callbackQuery.Id,"Sorry\n Cannot delete this message.",true);
            }
        }
        else
            OnCallback(client,msg);
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
            userDatabase.Insert(newUser!);
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
        _userDatabase = null;
        _groupDatabase = null;
        submiters.Clear();
    }
    public void AddCallbackFunc(in CallbackHandler<CallbackMsg> func)
    {
        var weakRef = new WeakReference<CallbackHandler<CallbackMsg>>(func);
        if (submiters.Any(x => x == weakRef))
            return;
        submiters.Add(weakRef);
    }
    public void OnCallback(in ITelegramBotClient client,CallbackMsg msg)
    {
        List<WeakReference<CallbackHandler<CallbackMsg>>> _submiters = new(submiters);
        bool success = false;
        foreach(var submiter in _submiters)
        {
            CallbackHandler<CallbackMsg>? foo;
            if(submiter.TryGetTarget(out foo))
            {
                var (isSuccess,isMatch) = foo(msg);
                if (isSuccess && isMatch)
                    submiters.Remove(submiter);
                success = isSuccess;
            }
            else
                submiters.Remove(submiter);
        }
        if(!success)
            client.AnswerCallbackQueryAsync(msg.Id, "Query had expired", true).Wait();
    }
}
