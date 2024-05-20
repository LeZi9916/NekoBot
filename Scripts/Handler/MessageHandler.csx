using NekoBot;
using NekoBot.Exceptions;
using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Action = System.Action;
using Message = NekoBot.Types.Message;
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
        SupportUpdate = new UpdateType[] 
        {
            UpdateType.Message
        },
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
    
    Message PreHandle(in ITelegramBotClient client, Update update)
    {
        var userMsg = Message.Parse(client, update.Message);
        UserDiscover(update);
        if (userMsg!.IsGroup)
            GroupDiscover(update);
        var msg = userMsg;
        var from = userDatabase!.Find(x => x.Id == msg.From.Id);
        userMsg.From = from!;
        userMsg.Group = groupDatabase!.Find(x => x.Id == msg.Chat.Id);

        return userMsg;
    }
    // 此处传入的userMsg为待处理的Message
    // From未被替换为NekoBot.User
    public Action? Handle(in ITelegramBotClient client, in List<IExtension> extensions, Update update)
    {
        if (update.Message is null)
            return default;
        var userMsg = PreHandle(client, update);        

        if (userMsg.Command is null)
            return default;

        var group = userMsg.Group;
        var user = userMsg.From;
        var cmd = (Command)userMsg.Command;

        // Reference check
        if (group is not null && group.Setting.ForceCheckReference)
        {
            if (!cmd.Prefix.Contains("@"))
                return default;

            var prefix = cmd.Prefix;
            var s = prefix.Split("@", 2, StringSplitOptions.RemoveEmptyEntries);

            if (s.Length != 2 || s[1] != Core.BotUsername)
                return default;
        }
        if (cmd.Prefix.Contains("@"))
        {
            cmd.Prefix = cmd.Prefix.Split("@", StringSplitOptions.RemoveEmptyEntries).First();
            userMsg.Command = cmd;
        }

        // Sender check
        if (!user.IsNormal)
        {
            var sArray = string.Join(" ", cmd.Params).Split("-token", StringSplitOptions.RemoveEmptyEntries);
            if (sArray.Length != 2)
            {
                //SendMessage("Authentication failed:\nGroup or channel anonymous access is not allowed", update);
                Debug(DebugType.Info, "Channel access,rejected");
                return default;
            }
            var token = sArray[1];

            if (!Core.Config.Authenticator.Compare(token.Trim()))
            {
                //SendMessage("Authentication failed:\nInvalid HOTP code", update);
                Debug(DebugType.Info, "HOTP code is invalid,rejected");
                return default;
            }
            else
                cmd.Params = sArray[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
        }
        // Banned user check
        if (!user.CheckPermission(Permission.Root) &&
            !user.CheckPermission(Permission.Common, group))
        {
            //SendMessage("Access Denied", update);
            Debug(DebugType.Info, "Banned user access,rejected");
            return default;
        }
        Debug(DebugType.Debug, "User Request:\n" +
            $"From: {userMsg.From.Name}({userMsg.From.Id})\n" +
            $"Chat: {userMsg.Chat.Id}\n" +
            $"Permission: {userMsg.From.Level.ToString()}\n" +
            $"Prefix: /{cmd.Prefix}\n" +
            $"Params: {string.Join(" ", cmd.Params)}");

        var module = extensions.Find(x => x.Info.Commands.Any(y => y.Command == cmd.Prefix) &&
                                     x.Info.Type == ExtensionType.Module &&
                                     x.Info.SupportUpdate.Contains(update.Type));
        if (module is null)
        {
            Debug(DebugType.Warning, $"No module support,this command will not be handled:\n" +
                                     $"Prefix : \"{cmd.Prefix}\"\n" +
                                     $"MsgType: {update.Type}\n" +
                                     $"ExtType: {ExtensionType.Module}");
            return default;
        }
        else
            return () => { module.Handle(userMsg); };
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

            if (groupDatabase.FindIndex(x => x.Id == groupId) != -1)
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
