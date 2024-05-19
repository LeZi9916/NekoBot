using NekoBot;
using NekoBot.Exceptions;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = NekoBot.Types.User;
using Version = NekoBot.Types.Version;

public class MessageHandler: ExtensionCore, IExtension, IHandler
{
    IDatabase<User>? userDatabase;
    IDatabase<Group>? groupDatabase;
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "MessageHandler",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Handler,
        Dependencies = new ExtensionInfo[]{
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
        }
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
    // 此处传入的userMsg为待处理的Message
    // From未被替换为NekoBot.User
    public void Handle(ref NekoBot.Types.Message userMsg)
    {
        if(userMsg.Raw is not null)
        {
            UserDiscover(userMsg.Raw);
            if (userMsg.IsGroup)
                GroupDiscover(userMsg.Raw);
        }
        var msg = userMsg;
        var from = userDatabase!.Find(x => x.Id == msg.From.Id);
        userMsg.From = from!;
        userMsg.Group = groupDatabase!.Find(x => x.Id == msg.Chat.Id);
    }
    void UserDiscover(in Update update)
    {
        if (userDatabase is null)
        {
            Debug(DebugType.Warning, $"User database not found, this change will not be take effect");
            return;
        }
        try
        {
            var userList = User.GetUsers(update);

            if (userList is null)
                return;

            foreach (var user in userList)
            {
                if (user is null)
                    continue;
                var record = userDatabase.Find(x => x.Id == user.Id);

                if (record is null)
                {
                    userDatabase.Add(user);
                    Debug(DebugType.Info, $"Find New User:\n" +
                    $"Name: {user.FirstName} {user.LastName}\n" +
                    $"isBot: {user.IsBot}\n" +
                    $"Username: {user.Username}\n" +
                    $"isPremium: {user.IsPremium}");
                }
                else if (record.Update(user))
                {
                    userDatabase.Update(x => x.Id == user.Id, record);
                    Debug(DebugType.Info, $"User info had been updated:\n" +
                    $"Name: {user.FirstName} {user.LastName}\n" +
                    $"isBot: {user.IsBot}\n" +
                    $"Username: {user.Username}\n" +
                    $"isPremium: {user.IsPremium}");
                }
            }
        }
        catch(Exception e)
        {
            Debug(DebugType.Error, $"Discover new user failure: \n{e}");
        }
    }
    void GroupDiscover(in Update update)
    {
        if (groupDatabase is null)
        {
            Debug(DebugType.Warning, $"Group database not found, this change will not be take effect");
            return;
        }
        try
        {
            var chat = update.Message?.Chat
            ?? update.EditedMessage?.Chat;

            if (chat is null)
                return;
            else if (chat.Type is not (ChatType.Group or ChatType.Supergroup))
                return;

            var groupId = chat.Id;

            if (Config.GroupIdList.Contains(groupId))
                return;

            var group = new Group()
            {
                Id = groupId,
                Name = chat.Title ?? "",
                Username = chat.Username ?? "",
            };
            groupDatabase.Add(group);
            Debug(DebugType.Debug, $"Find New Group:\n" +
                $"Name: {group.Name}\n" +
                $"Id: {groupId}\n" +
                $"Username：{group.Username}\n");
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Find a new group,but cannon add to db: \n{e.Message}\n{e.StackTrace}");
        }
    }
}
