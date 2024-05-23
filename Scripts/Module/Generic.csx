using CSScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NekoBot.Interfaces;
using NekoBot;
using NekoBot.Types;
using Version = NekoBot.Types.Version;
using Message = NekoBot.Types.Message;
using User = NekoBot.Types.User;
using Group = NekoBot.Types.Group;
using NekoBot.Exceptions;
using System.Diagnostics;

#pragma warning disable CS4014
public partial class Generic : Extension, IExtension
{
    IDatabase<User> userDatabase 
    { 
        get
        {
            var newDB = ScriptManager.GetExtension("UserDatabase");
            if(newDB is not null and IDatabase<User> db && 
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
            var newDB = ScriptManager.GetExtension("GroupDatabase");
            if (newDB is not null and IDatabase<Group> db && 
                db != _groupDatabase)
            {
                _groupDatabase = db;
            }
            return _groupDatabase ?? throw new DatabaseNotFoundException("This script depends on the database");
        }
    }
    IDatabase<User>? _userDatabase = new Database<User>();
    IDatabase<Group>? _groupDatabase = new Database<Group>();
    public new ExtensionInfo Info { get; } = new ExtensionInfo() 
    { 
        Name = "Generic",
        Version = new Version() { Major = 1, Minor = 0,Revision = 3 },
        Type = ExtensionType.Module,
        Commands = new BotCommand[] 
        {
            new BotCommand()
            {
                Command = "start",
                Description = "简介"
            },
            new BotCommand()
            {
                Command = "add",
                Description = "允许指定用户访问bot"
            },
            new BotCommand()
            {
                Command = "ban",
                Description = "禁止指定用户访问bot"
            },
            new BotCommand()
            {
                Command = "info",
                Description = "获取指定用户信息"
            },
            new BotCommand()
            {
                Command = "promote",
                Description = "提升指定用户权限"
            },
            new BotCommand()
            {
                Command = "demote",
                Description = "降低指定用户权限"
            },
            new BotCommand()
            {
                Command = "status",
                Description = "显示bot服务器状态"
            },
            new BotCommand()
            {
                Command = "logs",
                Description = "获取本次运行日志"
            },
            new BotCommand()
            {
                Command = "config",
                Description = "修改bot在Group的设置"
            },
            new BotCommand()
            {
                Command = "set",
                Description = "权限狗专用"
            },
            new BotCommand()
            {
                Command = "help",
                Description = "显示帮助信息"
            }
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
            },
            new ExtensionInfo()
            {
                Name = "Monitor",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Module
            }
        },
        SupportUpdate = new UpdateType[] 
        {
            UpdateType.Message,
            UpdateType.EditedMessage
        }

    };
    public override void Init()
    {
        _userDatabase = ((ScriptManager.GetExtension("UserDatabase") ?? throw new DatabaseNotFoundException("This script depends on the database to initialize")) 
                        as IDatabase<User>)!;
        _groupDatabase = ((ScriptManager.GetExtension("GroupDatabase") ?? throw new DatabaseNotFoundException("This script depends on the database to initialize")) 
                        as IDatabase<Group>)!;

        _userDatabase.OnDestroy += () => _userDatabase = null;
        _groupDatabase.OnDestroy += () => _groupDatabase = null;
    }
    public override void Handle(Message userMsg)
    {
        if(_userDatabase is null || _groupDatabase is null)
        {
            userMsg.Reply("Internal error: Module\"UserDatabase\" or \"GroupDatabase\" not found");
            return;
        }

        var cmd = (Command)userMsg.Command!;
        if (cmd.Prefix is "help")
        {
            GetHelpInfo((Command)userMsg.Command,userMsg);
            return;
        }
        switch (cmd.Prefix)
        {
            case "start":
                userMsg.Reply("你好，我是钟致远，我出生于广西壮族自治区南宁市，从小就擅长滥用，有关我的滥用事迹请移步 @hax_server\n" +
                    "\n请输入 /help 以获得更多信息");
                break;
            case "add":
                AddUser(userMsg);
                break;
            case "ban":
                BanUser(userMsg);
                break;
            case "status":
                GetSystemInfo(userMsg);
                break;
            case "info":
                GetUserInfo(userMsg);
                break;
            case "promote":
                SetUserPermission(userMsg, 1);
                break;
            case "demote":
                SetUserPermission(userMsg, -1);
                break;
            case "config":
                BotConfig(userMsg);
                break;
            case "help":
                GetHelpInfo((Command)userMsg.Command,userMsg);
                break;
            case "logs":
                GetBotLog(userMsg);
                break;
            case "set":
                AdvancedCommandHandle(userMsg);
                break;
        }
    }
    //bool MessageFilter(string content, Update update, TelegramBot.Types.User querier, Group group)
    //{
    //    if (group is null)
    //        return false;
    //    else if (group.Rules.IsEmpty())
    //        return false;

    //    bool isMatch = false;
    //    var genericRules = group.Rules.Where(x => x.Target is null).ToArray();
    //    var matchedRules = group.Rules.Where(x => x is not null && x.Target.Id == querier.Id).ToArray();
    //    FilterRule[] rules = new FilterRule[genericRules.Length + matchedRules.Length];
    //    Array.Copy(genericRules, rules, genericRules.Length);
    //    Array.Copy(matchedRules, 0, rules, genericRules.Length, matchedRules.Length);

    //    foreach (var rule in rules)
    //    {
    //        if (rule.Action is Action.Ban)
    //            continue;
    //        else if (rule.MessageType is MessageType.Unknown || rule.MessageType == update.Message.Type)
    //        {
    //            switch (rule.Action)
    //            {
    //                case Action.Reply:
    //                    break;
    //                case Action.Delete:
    //                    break;
    //            }
    //        }
    //        else
    //            continue;
    //    }
    //    return isMatch;
    //}
    void AddUser(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        var replyTo = userMsg.ReplyTo;
        User? target = null;

        if (param.IsEmpty())
        {
            if (replyTo is null)
            {
                GetHelpInfo(cmd,userMsg);
                return;
            }
            target = userDatabase.Find(x => x.Id ==replyTo.From.Id);
        }
        else
        {
            long id = -1;

            if (long.TryParse(param.First(), out id))
                target = userDatabase.Find(x => x.Id ==id);
            else
            {
                userMsg.Reply("Invild userId");
                return;
            }
        }

        if (target is null)
        {
            userMsg.Reply($"This user is not found at database");
            return;
        }
        else if (target.Id == querier.Id)
        {
            userMsg.Reply($"Not allow operation");
            return;
        }
        else if (target.Level >= Permission.Common)
        {
            userMsg.Reply($"This user had been allowed to access the bot");
            return;
        }
        else
        {
            target.SetPermission(Permission.Common);
            userMsg.Reply($"This user can access the bot now");
            return;
        }
    }
    void BanUser(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        var replyTo = userMsg.ReplyTo;
        User? target = null;

        if (param.IsEmpty())
        {
            if (replyTo is null)
            {
                GetHelpInfo(cmd,userMsg);
                return;
            }
            target = userDatabase.Find(x => x.Id ==replyTo.From.Id);
        }
        else
        {
            long id = -1;

            if (long.TryParse(param.First(), out id))
                target = userDatabase.Find(x => x.Id ==id);
            else
            {
                userMsg.Reply("Invild userId");
                return;
            }
        }

        if (target is null)
        {
            userMsg.Reply($"This user is not found at database");
            return;
        }
        else if (target.Id == querier.Id || target.Level >= querier.Level)
        {
            userMsg.Reply($"Not allow operation");
            return;
        }
        else
        {
            target.SetPermission(Permission.Ban);
            userMsg.Reply($"This user cannot access the bot now");
        }
    }
    void GetUserInfo(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.ToArray();

        var replyTo = userMsg.ReplyTo;
        User? target = null;

        if (!param.IsEmpty())
        {
            if (long.TryParse(param.First(), out long i))
            {
                target = userDatabase.Find(x => x.Id ==i);
            }
            else if (param.First().ToLower() == "group")
            {
                GetGroupInfo(userMsg);
                return;
            }
            else
            {
                GetHelpInfo(cmd,userMsg);
                return;
            }
        }
        else if(replyTo is not null)
            target = replyTo.From;
        else
            target = userMsg.From;

        if (target is null)
            userMsg.Reply("User not found at database");
        else if (target.Id != querier.Id && !querier.CheckPermission(Permission.Admin))
            userMsg.Reply("Permission denied.");
        else
            userMsg.Reply($"User Infomation:\n```copy\n" + StringHandle(
                          $"Name      : {target.Name}\n" +
                          $"Id        : {target.Id}\n" +
                          $"Permission: {target.Level}\n" +
                          $"MaiUserId : {(userMsg.IsGroup ? 
                                         (target.MaiUserId is null ? "未绑定" : "喵") : 
                                         (target.MaiUserId is null ? "未绑定" : target.MaiUserId))}\n")+"```", ParseMode.MarkdownV2);




    }
    void GetGroupInfo(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();
        var group = userMsg.Group;

        var replyTo = userMsg.ReplyTo;
        Group? target = null;

        if(!userMsg.IsGroup)
        {
            userMsg.Reply("This function only can use at group");
            return;
        }
        else if (!param.IsEmpty())
        {
            if (long.TryParse(param.First(), out long i))
               target = groupDatabase.Find(x => x.Id == i);
            else
            {
                GetHelpInfo(cmd, userMsg);
                return;
            }
        }
        else
            target = userMsg.Group;

        if (target is null)
            userMsg.Reply("User not found at database");
        else if (target.Id != group!.Id && !querier.CheckPermission(Permission.Admin))
            userMsg.Reply("Permission denied.");
        else
            userMsg.Reply($"User Infomation:\n```copy\n" + StringHandle(
                          $"Name      : {target.Name}\n" +
                          $"Id        : {target.Id}\n" +
                          $"Permission: {target.Level}\n\n" +
                          $"Group Setting:\n" +
                          $"Force Check Reference: {target.Setting.ForceCheckReference}\n" +
                          $"Listening : {target.Setting.Listen}") + "```", ParseMode.MarkdownV2);
    }
    void SetUserPermission(Message userMsg, int diff)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        var replyTo = userMsg.ReplyTo;
        Func<Permission, User, bool> canPromote = (s, user) =>
        {
            switch (s)
            {
                case Permission.Unknown:
                case Permission.Ban:
                case Permission.Common:
                case Permission.Advanced:
                    return user.CheckPermission(Permission.Admin);
                case Permission.Admin:
                case Permission.Root:
                    return user.CheckPermission(Permission.Root);
                default:
                    return false;
            }
        };
        Action<long> setPermission = id =>
        {
            if (id == querier.Id)
            {
                userMsg.Reply("Cannot change permission for yourself");
                return;
            }

            var target = userDatabase.Find(x => x.Id ==id);
            var targetLevel = target!.Level + diff;

            if (target is null)
            {
                userMsg.Reply($"This user is not found at database");
                return;
            }
            if (target.IsUnknown)
            {
                userMsg.Reply($"Cannot change permission for this user");
                return;
            }
            if (canPromote(targetLevel, querier))
            {
                if (targetLevel < Permission.Ban)
                {
                    userMsg.Reply($"This user permission is already the lowest");
                    return;
                }
                else if (targetLevel > Permission.Admin)
                {
                    userMsg.Reply($"This user permission is already the highest");
                    return;
                }

                if (target.Level >= querier.Level)
                {
                    userMsg.Reply($"Permission Denied");
                    return;
                }
                target.SetPermission(targetLevel);
                userMsg.Reply($"Success change*{StringHandle(target.Name)}*\\({target.Id}\\) permission to *{targetLevel}*",parseMode: ParseMode.MarkdownV2);
            }
            else
            {
                userMsg.Reply("Permission Denied");
                return;
            }
        };

        if (replyTo is not null)
            setPermission(replyTo.From.Id);
        else if(!param.IsEmpty())
        {
            long id = -1;

            if (long.TryParse(param.First(), out id))
                setPermission(id);
            else
                userMsg.Reply("Invaild userId");
        }
        else
            userMsg.Reply("Require user");
    }
    async void GetSystemInfo(Message userMsg)
    {
        var uptime = DateTime.Now - Config.Up;
        var scripts = string.Join("\n  - ", ScriptManager.GetLoadedScript());
        var analyzer = Core.Config.Analyzer;
        var extension = ScriptManager.GetExtension("Monitor");

        if (extension is IMonitor<Dictionary<string, long>> monitor)
        {
            var result = monitor.GetResult();
            string _ = $"""
                        - NekoBot info 
                          Version  : v{ScriptManager.GetVersion()}
                          MemUsage : {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)} MB
                          Latency  : {Math.Round(analyzer.TotalHandleTime / (double)analyzer.TotalHandleCount,2)} ms
                          Processed Msg : {analyzer.TotalHandleCount}
                        - HW info
                          - CPU
                            Core : {result["ProcessorCount"]}
                            Usage: {result["CPULoad"]}%
                            - Avg
                              5m: {result["_5CPULoad"]}%
                             10m: {result["_10CPULoad"]}%
                             15m: {result["_15CPULoad"]}%
                          - Memory
                            Total: {result["TotalMemory"] / 1000000} MB
                            Free : {result["FreeMemory"] / 1000000} MB
                            Usage: {result["UsedMemory"] / 1000000} MB ({result["UsedMemory"] * 100 / result["TotalMemory"]}%)
                        - Scripts
                          - {scripts}
                        """;
            await userMsg.Reply($"""
                                 ```python
                                 {StringHandle(_)}
                                 ```
                                 """, ParseMode.MarkdownV2);
        }
        else
            userMsg.Reply("Internal error: Module\"Monitor\" not found");
    }
    void GetBotLog(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();
        int count = 15;
        DebugType? level = DebugType.Error;

        if (!querier.CheckPermission(Permission.Root))
        {
            userMsg.Reply("Permission Denied");
            return;
        }
        switch (param.Length)
        {
            case 0:
                break;
            case 1:
                level = param.First() switch
                {
                    "debug" => DebugType.Debug,
                    "info" => DebugType.Info,
                    "warning" => DebugType.Warning,
                    "err" => DebugType.Error,
                    _ => null
                };
                if (level is null)
                {
                    if (!int.TryParse(param.First(), out count))
                    {
                        userMsg.Reply($"Invaild param: \"{param.First()}\"");
                        return;
                    }
                    level = DebugType.Error;
                }
                break;
            case 2:
                level = param.First() switch
                {
                    "debug" => DebugType.Debug,
                    "info" => DebugType.Info,
                    "warning" => DebugType.Warning,
                    "err" => DebugType.Error,
                    _ => null
                };
                if (level is null)
                {
                    userMsg.Reply($"Invaild param: \"{param[0]}\"");
                    return;
                }
                else if (!int.TryParse(param[1], out count))
                {
                    userMsg.Reply($"Invaild param: \"{param[1]}\"");
                    return;
                }
                break;

        }

        if (level < DebugType.Warning)
        {
            if (!querier.CheckPermission(Permission.Admin))
            {
                userMsg.Reply("Permission Denied");
                return;
            }
        }
        var logs = LogManager.GetLog(count, (DebugType)level);

        if (logs.IsEmpty())
            userMsg.Reply("No logs");
        else
        {
            userMsg.Reply(
                $"""
                ```csharp
                {StringHandle(string.Join("\n", logs.Select(x => x.ToString())))}
                ```
                """, ParseMode.MarkdownV2);
        }
    }
    void BotConfig(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params;
        var group = userMsg.Group;

        if (!userMsg.IsGroup)
        {
            userMsg.Reply("Only can use in group");
            return;
        }
        if (!querier.CheckPermission(Permission.Admin, group))
        {
            userMsg.Reply($"Permission Denied");
            return;
        }
        if (param.Length < 2)
        {
            GetHelpInfo(cmd,userMsg);
            return;
        }

        string prefix = param.First().ToLower();
        bool boolValue;
        switch (prefix)
        {
            case "forcecheckreference":
                if (bool.TryParse(param[1], out boolValue))
                {
                    group!.Setting.ForceCheckReference = boolValue;
                    userMsg.Reply($"ForceCheckReference: *{boolValue}*",ParseMode.MarkdownV2);
                    //Config.SaveData();
                }
                else
                    GetHelpInfo(cmd,userMsg);
                break;
        }
    }
    internal void GetHelpInfo(Command cmd,Message userMsg)
    {
        string helpStr = "```python\n";
        switch (cmd.Prefix)
        {
            case "add":
                helpStr += StringHandle(
                    "命令用法:\n" +
                    "\n/add        允许reply对象访问bot" +
                    "\n/add [int]  允许指定用户访问bot");
                break;
            case "ban":
                helpStr += StringHandle(
                    "命令用法:\n" +
                    "\n/ban        封禁reply对象" +
                    "\n/ban [int]  封禁指定用户");
                break;
            case "info":
                helpStr += StringHandle(
                    "命令用法:\n" +
                    "\n/info        获取reply对象的用户信息" +
                    "\n/info [int]  获取指定用户的用户信息");
                break;
            case "promote":
                helpStr += StringHandle(
                    "命令用法:\n" +
                    "\n/promote        提升reply对象的权限等级" +
                    "\n/promote [int]  提升指定用户的权限等级");
                break;
            case "demote":
                helpStr += StringHandle(
                    "命令用法:\n" +
                    "\n/demote        降低reply对象的权限等级" +
                    "\n/demote [int]  降低指定用户的权限等级");
                break;
            case "help":
                helpStr += StringHandle(
                    "\n相关命令：\n" +
                    "\n/add        允许指定用户访问bot" +
                    "\n/ban        禁止指定用户访问bot" +
                    "\n/info       获取指定用户信息" +
                    "\n/promote    提升指定用户权限" +
                    "\n/demote     降低指定用户权限" +
                    "\n/status     显示bot服务器状态" +
                    "\n/logs       获取本次运行日志" +
                    "\n/maistatus  查看土豆服务器状况" +
                    "\n/set        权限狗专用" +
                    "\n/help       显示帮助信息\n" +
                    "\n更详细的信息请输入\"/{command} help\"");
                break;
            case "logs":
                helpStr += StringHandle(
                        "命令用法：\n" +
                        "\n/logs            获取最近15条等级为Error或以上的日志信息" +
                        "\n/logs [Lv|int]   获取自定义数量或等级的日志信息" +
                        "\n/logs [Lv] [int] 获取最近指定数量和等级的日志信息\n\n" +
                        "Lv参数的可用值:\n" +
                        "debug\n" +
                        "info\n" +
                        "warning\n" +
                        "err");
                break;
            default:
                userMsg.Reply("No helper");
                return;
        }
        helpStr += "\n```";
        userMsg.Reply(helpStr,ParseMode.MarkdownV2);
    }
    static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    static Permission GetPermission(string s) => 
        s.ToLower() switch
        {
            "ban" => Permission.Ban,
            "common" => Permission.Common,
            "advanced" => Permission.Advanced,
            "admin" => Permission.Admin,
            "root" => Permission.Root,
            _ => Permission.Unknown
        };
}
public partial class Generic
{
    void AdvancedCommandHandle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var suffix = cmd.Params.First();
        var param = cmd.Params.Skip(1).ToArray();

        if (!querier.CheckPermission(Permission.Root))
        {
            userMsg.Reply("Permission Denied");
            return;
        }

        
        switch (suffix)
        {
            case "permission":
                UserPermissionModify(userMsg);
                break;
                //case "maiuserid":
                //    SetScannerUpdate(command, update, querier);
                //    break;
        }
    }
    void UserPermissionModify(Message userMsg)
    {
        // /set permission [Group] [id] [level] 
        // /set permission [id] [level] 
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var suffix = cmd.Params.First();
        var param = cmd.Params.Skip(1).ToArray();
        string[] permission = { "unknown","ban","common","advanced","admin","root" };

        Permission targetLevel;

        if (param[0].ToLower() is "group" && param.Length == 3)
        {
            long id;
            if (!long.TryParse(param[1], out id))
            {
                userMsg.Reply("Invaild groupId");
                return;
            }

            Group? group = groupDatabase.Find(x => x.Id == id);
            if(group is null)
            {
                userMsg.Reply("Group not found at database");
                return;
            }
            targetLevel = GetPermission(param[2]);

            if (!permission.Contains(param[2]))
            {
                userMsg.Reply($"Unknown permission: \"{param[2]}\"");
                return;
            }
            group.SetPermission(targetLevel);
            userMsg.Reply($"Success change*{StringHandle(group.Name)}*\\({group.Id}\\) permission to *{targetLevel}*", parseMode: ParseMode.MarkdownV2);
        }
        else if(param.Length == 2)
        {
            long id;
            if (!long.TryParse(param[0],out id))
            {
                userMsg.Reply("Invaild userId");
                return;
            }

            User? user = userDatabase.Find(x => x.Id == id);
            if (user is null)
            {
                userMsg.Reply("User not found at database");
                return;
            }
            targetLevel = GetPermission(param[1]);

            if(!permission.Contains(param[1]))
            {
                userMsg.Reply($"Unknown permission: \"{param[1]}\"");
                return;
            }
            else if(userMsg.From.Id == id)
            {
                userMsg.Reply($"Cannot use for yourself");
                return;
            }

            user.SetPermission(targetLevel);
            userMsg.Reply($"Success change*{StringHandle(user.Name)}*\\({user.Id}\\) permission to *{targetLevel}*", parseMode: ParseMode.MarkdownV2);
        }
        else
            GetHelpInfo(cmd, userMsg);



    }
}
