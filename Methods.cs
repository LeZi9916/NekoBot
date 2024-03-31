using CSScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Class;

namespace TelegramBot
{
    internal partial class Program
    {
        static void AddUser(Command command, Update update, TUser querier, Group group = null)
        {
            var replyMessage = update.Message.ReplyToMessage;
            TUser target = null; 
            
            if (command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    GetHelpInfo(command,update,querier);
                    return;
                }
                target = Config.SearchUser(replyMessage.From.Id);
            }
            else
            {
                long id = -1;

                if (long.TryParse(command.Content[0], out id))
                    target = Config.SearchUser(id);
                else
                {
                    SendMessage("userId错误，请仔细检查喵~", update);
                    return;
                }
            }

            if(target is null)
            {
                SendMessage($"喵不认识这个人xAx", update);
                return;
            }
            else if (target.Id == querier.Id)
            {
                SendMessage($"喵喵喵？QAQ", update);
                return;
            }
            else if(target.Level >= Permission.Common)
            {
                SendMessage($"这位朋友咱已经认识啦~", update);
                return;
            }
            else
            {
                target.SetPermission(Permission.Common);
                SendMessage($"欢迎新朋友~", update);
            }
        }
        static void BanUser(Command command, Update update, TUser querier, Group group = null)
        {
            var replyMessage = update.Message.ReplyToMessage;
            TUser target = null;

            if (command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    GetHelpInfo(command, update, querier);
                    return;
                }
                target = Config.SearchUser(replyMessage.From.Id);
            }
            else
            {
                long id = -1;

                if (long.TryParse(command.Content[0], out id))
                    target = Config.SearchUser(id);
                else
                {
                    SendMessage("userId错误，请仔细检查喵~", update);
                    return;
                }
            }

            if (target is null)
            {
                SendMessage($"喵不认识这个人xAx", update);
                return;
            }
            else if (target.Id == querier.Id)
            {
                SendMessage($"喵喵喵？QAQ", update);
                return;
            }
            else if (target.Level >= querier.Level)
            {
                SendMessage($"你想篡权喵?", update);
                return;
            }
            else
            {
                target.SetPermission(Permission.Ban);
                SendMessage($"已经将坏蛋踢出去了喵", update);
            }
        }
        static void GetUserInfo(Command command, Update update,TUser querier, Group group = null)
        {
            var isGroup = update.Message.Chat.Type is (ChatType.Group or ChatType.Supergroup);
            var id = (update.Message.ReplyToMessage is not null ? (update.Message.ReplyToMessage.From ?? update.Message.From) : update.Message.From).Id;


            if (id != querier.Id && !querier.CheckPermission(Permission.Admin))
            {
                SendMessage("Permission denied.", update);
                return;
            }
            else if (command.Content.Length > 0)
            {
                if(command.Content[0] is "group")
                {
                    if(group is null)
                        SendMessage("获取群组信息时发生错误QAQ:\n此功能只能在群组内使用", update);
                    else
                    {
                        SendMessage(
                            $"群组信息:\n" +
                            $"Name: {group.Name}\n" +
                            $"Id: {group.Id}\n" +
                            $"Permission: {group.Level}", update);
                    }
                }
                else if (!long.TryParse(command.Content[0], out id))
                {
                    GetHelpInfo(command, update, querier);
                    return;
                }
            }
            else
            {
                var target = Config.SearchUser(id);

                if (target is null)
                    SendMessage("查无此人喵", update);
                else
                    SendMessage(
                        $"用户信息:\n" +
                        $"Name: {target.Name}\n" +
                        $"Id: {target.Id}\n" +
                        $"Permission: {target.Level}\n" +
                        $"MaiUserId: {(isGroup ? target.MaiUserId is null ? "未绑定" : "喵" : target.MaiUserId is null ? "未绑定" : target.MaiUserId)}", update);
            }
            
            

            
        }
        static void SetUserPermission(Command command, Update update, TUser querier,int diff, Group group = null)
        {
            var replyMessage = update.Message.ReplyToMessage;
            Func<Permission,TUser, bool> canPromote = (s,user) => 
            {
                switch(s)
                {
                    case Permission.Unknow:
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
            Func<string, Permission> getLevel = s =>
            {
                return s.ToLower() switch
                {
                    "ban" => Permission.Ban,
                    "common" => Permission.Common,
                    "admin" => Permission.Admin,
                    "root" => Permission.Root,
                    _ => Permission.Unknow
                };
            };
            Action<long> setPermission = id =>
            {
                if (id == querier.Id)
                {
                    SendMessage("不可以修改自己的权限喵",update);
                    return;
                }

                var target = Config.SearchUser(id);
                var targetLevel = target.Level + diff;

                if(target is null)
                {
                    SendMessage($"喵不认识这个人xAx", update);
                    return;
                }
                if(target.isUnknow)
                {
                    SendMessage($"无效操作喵", update);
                    return;
                }
                if (canPromote(targetLevel, querier))
                {
                    if(targetLevel < Permission.Ban)
                    {
                        SendMessage($"权限已经不能再降低了QAQ", update);
                        return;
                    }
                    else if (targetLevel > Permission.Admin)
                    {
                        SendMessage($"权限已经不能再提高了QAQ", update);
                        return;
                    }

                    if (target.Level >= querier.Level)
                    {
                        SendMessage($"你不可以修改 *{StringHandle(target.Name)}*的权限喵xAx", update, parseMode: ParseMode.MarkdownV2);
                        return;
                    }
                    target.SetPermission(targetLevel);
                    SendMessage($"成功将*{StringHandle(target.Name)}*\\({target.Id}\\)的权限修改为*{targetLevel}*", update, parseMode:ParseMode.MarkdownV2);
                }
                else
                {
                    SendMessage("很抱歉，您的权限不足喵", update);
                    return;
                }
            };

            if(command.Content.Length == 0)
            {
                if (replyMessage is null)
                {
                    SendMessage("请指明需要修改的用户喵", update);
                    return;
                }
                setPermission(replyMessage.From.Id);                
            }
            else if (command.Content.Length == 1)
            {
                long id = -1;

                if(long.TryParse(command.Content[0],out id))
                    setPermission(id);
                else
                    SendMessage("userId错误，请仔细检查喵~", update);
            }
            else
                SendMessage("喵?", update);
        }        
        static async void GetSystemInfo(Command command, Update update)
        {
            var uptime = DateTime.Now - startTime;
            await SendMessage(StringHandle(
                $"当前版本: v{Assembly.GetExecutingAssembly().GetName().Version}\n\n" +
                "系统信息:\n" +
                $"-核心数: {Monitor.ProcessorCount}\n" +
                $"-使用率: {Monitor.CPULoad}%\n" +
                $"-总内存: {Monitor.TotalMemory/1000000} MiB\n" +
                $"-剩余内存: {Monitor.FreeMemory/1000000} MiB\n" +
                $"-已用内存: {Monitor.UsedMemory/1000000} MiB ({Monitor.UsedMemory * 100 / Monitor.TotalMemory }%)\n" +
                $"-在线时间: {uptime.Hours}h{uptime.Minutes}m{uptime.Seconds}s\n\n" +
                $"-总计处理消息数: {Config.TotalHandleCount}\n" +
                $"-平均耗时: {(Config.TotalHandleCount is 0 ? 0 :Config.TimeSpentList.Sum() / Config.TotalHandleCount)}ms\n" +
                $"-5分钟平均CPU占用率: {Monitor._5CPULoad}%\n" +
                $"-10分钟平均CPU占用率: {Monitor._10CPULoad}%\n" +
                $"-15分钟平均CPU占用率: {Monitor._15CPULoad}%\n"),update,true,ParseMode.MarkdownV2);
        }
        static async void GetBotLog(Command command, Update update, TUser querier, Group group = null)
        {
            var message = update.Message;
            var chat = update.Message.Chat;
            var isPrivate = chat.Type is ChatType.Private;
            int count = 15;
            DebugType? level = DebugType.Error;

            //if(!isPrivate)
            //{
            //    SendMessage("喵呜呜", update, true);
            //    return;
            //}
            if(!querier.CheckPermission(Permission.Root))
            {
                SendMessage("喵?", update, true);
                return;
            }
            switch(command.Content.Length)
            {
                case 0:
                    break;
                case 1:
                    level = command.Content[0] switch
                    {
                        "debug" => DebugType.Debug,
                        "info" => DebugType.Info,
                        "warning" => DebugType.Warning,
                        "err" => DebugType.Error,
                        _ => null
                    };
                    if(level is null)
                    {
                        if(!int.TryParse(command.Content[0],out count))
                        {
                            SendMessage($"\"{command.Content[0]}\"不是有效参数喵x", update);
                            return;
                        }
                        level = DebugType.Error;
                    }
                    break;
                case 2:
                    level = command.Content[0] switch
                    {
                        "debug" => DebugType.Debug,
                        "info" => DebugType.Info,
                        "warning" => DebugType.Warning,
                        "err" => DebugType.Error,
                        _ => null
                    };
                    if (level is null)
                    {
                        SendMessage($"\"{command.Content[0]}\"不是有效参数喵x", update);
                        return;
                    }
                    else if (!int.TryParse(command.Content[1], out count))
                    {
                        SendMessage($"\"{command.Content[1]}\"不是有效参数喵x", update);
                        return;
                    }
                    break;

            }

            if(level < DebugType.Warning)
            {
                if (!querier.CheckPermission(Permission.Admin))
                {
                    SendMessage("Permission Denied", update, true);
                    return;
                }
            }

            var logs = LogManager.GetLog(count,(DebugType)level);

            if (logs.IsEmpty())
                SendMessage("暂无可用日志喵", update);
            else
            {
                var logText = string.Join("", logs).Replace("\\", "\\\\");
                if(logText.Length > 4000)
                {
                    //int index = 0;
                    //string[] msgGroup = new string[(int)Math.Ceiling((double)logText.Length / 4000)];
                    //while(index * 4000 < logText.Length)
                    //{
                    //    msgGroup[index] = logText.Substring(index * 4000, Math.Min(4000,logText.Length - 1 - index * 4000));
                    //    index++;
                    //}

                    //foreach(var s in msgGroup)
                    //    await SendMessage("```csharp\n" +
                    //         $"{StringHandle(s)}\n" +
                    //         $"```", update, true, ParseMode.MarkdownV2);

                    List<string> msgGroup = new();
                    string msg = "";

                    foreach(var s in logs)
                    {
                        if (($"{msg}{s.Replace("\\", "\\\\")}").Length > 4000)
                        {
                            msgGroup.Add(msg);
                            msg = $"{s.Replace("\\", "\\\\")}";
                        }
                        else
                            msg += $"{s.Replace("\\", "\\\\")}";
                    }
                    msgGroup.Add(msg);
                    foreach (var s in msgGroup)
                        await SendMessage("```csharp\n" +
                             $"{StringHandle(s)}\n" +
                             $"```", update, true, ParseMode.MarkdownV2);
                }
                else
                    await SendMessage("```csharp\n" +
                             $"{StringHandle(logText)}\n" +
                             $"```", update, true, ParseMode.MarkdownV2);
            }
            //await UploadFile(Config.LogFile,chat.Id);
        }
        static void BotConfig(Command command, Update update, TUser querier, Group group = null)
        {
            if(group is null)
            {
                SendMessage("此功能只能在Group里面使用喵x",update);
                return;
            }
            if(!querier.CheckPermission(Permission.Admin, group))
            {
                SendMessage($"很抱歉，您不能修改bot的设置喵", update);
                return;
            }
            if(command.Content.Length < 2)
            {
                GetHelpInfo(command, update, querier);
                return;
            }

            string prefix = command.Content[0].ToLower();
            bool boolValue;
            switch(prefix)
            {
                case "forcecheckreference":
                    if (bool.TryParse(command.Content[1], out boolValue))
                    {
                        group.Setting.ForceCheckReference = boolValue;
                        SendMessage($"已将ForceCheckReference属性修改为*{boolValue}*",update,true,ParseMode.MarkdownV2);
                        Config.SaveData();
                    }
                    else
                        GetHelpInfo(command, update, querier);
                    break;
            }
        }
        static void GetHelpInfo(Command command, Update update, TUser querier, Group group = null)
        {
            var isPrivate = update.Message.Chat.Type is ChatType.Private;
            string helpStr = "```python\n";
            switch (command.Prefix)
            {
                case CommandType.Add:
                    helpStr += StringHandle(
                        "命令用法:\n" +
                        "\n/add        允许reply对象访问bot" +
                        "\n/add [int]  允许指定用户访问bot");
                    break;
                case CommandType.Ban:
                    helpStr += StringHandle(
                        "命令用法:\n" +
                        "\n/ban        封禁reply对象" +
                        "\n/ban [int]  封禁指定用户");
                    break;
                case CommandType.Info:
                    helpStr += StringHandle(
                        "命令用法:\n" +
                        "\n/info        获取reply对象的用户信息" +
                        "\n/info [int]  获取指定用户的用户信息");
                    break;
                case CommandType.Promote:
                    helpStr += StringHandle(
                        "命令用法:\n" +
                        "\n/promote        提升reply对象的权限等级" +
                        "\n/promote [int]  提升指定用户的权限等级");
                    break;
                case CommandType.Demote:
                    helpStr += StringHandle(
                        "命令用法:\n" +
                        "\n/demote        降低reply对象的权限等级" +
                        "\n/demote [int]  降低指定用户的权限等级");
                    break;
                case CommandType.Help:
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
                case CommandType.Mai:
                    helpStr += StringHandle(
                            "命令用法：\n" +
                            "\n/mai bind image    上传二维码并进行绑定" +
                            "\n/mai bind [str]    使用SDWC标识符进行绑定" +
                            "\n/mai region        获取登录地区信息" +
                            "\n/mai rank          获取国服排行榜" +
                            "\n/mai rank refresh  重新加载排行榜" +
                            "\n/mai status        查看DX服务器状态" +
                            "\n/mai backup [str]  使用密码备份账号数据" +
                            "\n/mai info          获取账号信息" +
                            "\n/mai info [int]    获取指定账号信息" +
                            "\n/mai ticket [int]  获取一张指定类型的票" +
                            "\n/mai sync          强制刷新账号信息" +
                            "\n/mai sync [int]    强制刷新指定账号信息" +
                            "\n/mai logout        登出");
                    break;
                case CommandType.MaiScanner:
                    helpStr += StringHandle(
                            "命令用法：\n" +
                            "\n/maiscanner status       获取扫描器状态" +
                            "\n/maiscanner update [int] 从指定位置更新数据库" +
                            "\n/maiscanner update       更新数据库" +
                            "\n/maiscanner stop         终止当前任务" +
                            "\n/maiscanner set [int]    设置QPS限制");
                    break;
                case CommandType.Logs:
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
                    SendMessage("该命令暂未添加说明信息喵x", update);
                    return;
            }
            helpStr += "\n```";
            SendMessage(helpStr, update, true, ParseMode.MarkdownV2);
        }
        static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
        
    }
    internal partial class Program
    {
        static void AdvancedCommandHandle(Command command, Update update, TUser querier, Group group = null)
        {
            if (!querier.CheckPermission(Permission.Root))
            {
                SendMessage("喵?", update);
                return;
            }

            var suffix = command.Content[0];
            command.Content = command.Content.Skip(1).ToArray();
            switch (suffix)
            {
                case "permission":
                    UserPermissionModify(command, update, querier);
                    break;
                //case "maiuserid":
                //    SetScannerUpdate(command, update, querier);
                //    break;
            }
        }
        static void UserPermissionModify(Command command, Update update, TUser querier)
        {
            var chat = update.Message.Chat;
            var chatType = chat.Type;
            var isGroup = chatType is (ChatType.Group or ChatType.Supergroup);

            long userId = 0;
            int targetLevel = 0;
            if(command.Content.Length < 2)
            {
                SendMessage("缺少参数喵x", update);
                return;
            }
            if (!long.TryParse(command.Content[0], out userId))
            {
                if (command.Content[0] == "group")
                {
                    userId = -1;
                    if(!isGroup)
                    {
                        SendMessage($"此参数仅对Group有效x", update);
                        return;
                    }
                }
                else
                {
                    SendMessage($"\"{command.Content[0]}\"不是有效的参数x",update);
                    return;
                }
            }
            if(!int.TryParse(command.Content[1], out targetLevel))
            {
                SendMessage($"\"{command.Content[1]}\"不是有效的参数x", update);
                return;
            }
            else if(targetLevel > (int)Permission.Root || targetLevel < (int)Permission.Unknow)
            {
                SendMessage($"没有这个权限喵QAQ", update);
                return;
            }

            if(userId == -1)
            {
                var group = Config.SearchGroup(chat.Id);
                Program.Debug(DebugType.Warning, $"Group not found,Group Id is \"{chat.Id}\"");

                if(group is null)
                {
                    SendMessage("发生内部错误QAQ", update);
                    Program.Debug(DebugType.Warning, $"Group not found,Group Id is \"{chat.Id}\"");
                    return;
                }

                group.SetPermission((Permission)targetLevel);
                SendMessage($"已将该群组的权限修改为*{(Permission)targetLevel}*喵",update,true,ParseMode.MarkdownV2);
                Config.SaveData();
            }
            else
            {
                var user = Config.SearchUser(userId);
                
                if (user is null)
                {
                    SendMessage("发生内部错误QAQ", update);
                    Program.Debug(DebugType.Warning, $"TUser not found,User Id is \"{userId}\"");
                    return;
                }

                user.SetPermission((Permission)targetLevel);
                SendMessage($"已将*{StringHandle(user.Name)}*的权限修改为*{(Permission)targetLevel}*喵", update, true, ParseMode.MarkdownV2);
                Config.SaveData();
            }

        }
    }


}
