using AquaTools.Exception;
using AquaTools.Requests;
using AquaTools.Responses;
using AquaTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using TelegramBot.Interfaces;
using System.Reflection;
using Microsoft.CodeAnalysis;
using TelegramBot.Types;
using CSScripting;
using static TelegramBot.Config;
using static TelegramBot.ChartHelper;
using Message = TelegramBot.Types.Message;
using File = System.IO.File;
using MaiAccount = TelegramBot.Types.MaiAccount;
#pragma warning disable CS4014
public partial class Mai : ExtensionCore, IExtension
{
    static MaiMonitor monitor;
    static MaiScanner scanner;
    static MaiDatabase database;

    public new BotCommand[] Commands { get; } =
    {
            new BotCommand()
            {
                Command = "mai",
                Description = "是什么呢？"
            },
            new BotCommand()
            {
                Command = "maiscanner",
                Description = "是什么呢？"
            },
            new BotCommand()
            {
                Command = "maistatus",
                Description = "查看土豆服务器状态"
            }
        };
    public string Name { get; } = "Mai";
    public override void Init()
    {
        monitor = new();
        scanner = new();
        database = new();

        database.Init();
        scanner.Init();
        monitor.Init();

    }
    public override void Save()
    {
        database.Save();
        monitor.Save();
    }
    public override void Destroy()
    {
        monitor.Destroy();
        scanner.Destroy();
        database = null;
        monitor = null;
        scanner = null;
    }
    public override void Handle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var group = userMsg.GetGroup();

        if (cmd.Prefix == "maistatus")
        {
            GetServerStatus(userMsg);
            return;
        }
        else if (!querier.CheckPermission(Permission.Advanced, group))
        {
            userMsg.Send("Permission Denied");
            return;
        }
        else if (cmd.Params.IsEmpty())
        {
            GetHelpInfo(cmd,userMsg);
            return;
        }

        var suffix = cmd.Params.First();
        if (suffix is not ("bind" or "status" or "rank") && querier.MaiUserId is null)
        {
            userMsg.Send("你还没有绑定账号喵x");
            return;
        }
        switch (suffix)
        {
            case "status":
                GetServerStatus(userMsg);
                break;
            case "region":
                GetUserRegion(userMsg);
                break;
            case "info":
                GetUserInfo(userMsg);
                break;
            case "bind":
                BindUser(userMsg);
                break;
            case "rank":
                GetTopRank(userMsg);
                break;
            case "logout":
                Logout(userMsg);
                break;
            case "backup":
                DataBackup(userMsg);
                break;
            case "sync":
                UpdateUserData(userMsg);
                break;
            case "ticket":
                //GetTicket(userMsg);
                break;
            case "maiscanner":
                break;
                //case "upsert":
                //    MaiUpsert(userMsg);
                //    break;
        }
    }
    internal void GetMaiUserId(Message userMsg)
    {

    }
    /// <summary>
    /// 获取maimai账号信息
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void GetUserInfo(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();
        try
        {
            querier.Account ??= database.Search((int)querier.Id);
            MaiAccount account = querier.Account;
            async Task<MaiAccount> getAccount(int userid)
            {
                var response = (await GetUserPreview((int)querier.MaiUserId)).Object;

                if (response.StatusCode is HttpStatusCode.OK)
                {
                    var maiAccount = new MaiAccount();
                    maiAccount.userName = StringHandle(response.userName);
                    maiAccount.playerRating = response.playerRating ?? 0;
                    maiAccount.userId = (int)querier.MaiUserId;
                    maiAccount.lastDataVersion = response.lastDataVersion;
                    maiAccount.lastRomVersion = response.lastRomVersion;
                    maiAccount.lastGameId = response.lastGameId;
                    maiAccount.banState = response.banState;
                    maiAccount.lastUpdate = DateTime.Now;

                    database.MaiAccountList.Add(maiAccount);
                    Config.SaveData();
                    return maiAccount;
                }
                else
                {
                    userMsg.Send("获取数据失败QAQ");
                    return null;
                }
            }

            if (!param.IsEmpty())
            {
                int id;
                if (!querier.CheckPermission(Permission.Admin))
                {
                    userMsg.Send("Access denied");
                    return;
                }
                else if (!int.TryParse(param.First(), out id))
                {
                    userMsg.Send("请确认参数是Int32~");
                    return;
                }

                account = database.Search(id);

                if (account is null)
                    account = await getAccount(id);
            }
            else if (cmd.Params.Length > 1)
            {
                userMsg.Send("参数错误QAQ");
                return;
            }
            else
            {
                if (account is null)
                {
                    querier.Account = await getAccount((int)querier.MaiUserId);
                    account = querier.Account;
                }
            }

            var msg = await userMsg.Send(
                "用户信息:\n" +
                $"名称: {account.userName}\n" +
                $"Rating: {account.playerRating}\n" +
                $"排名: 计算中...\n" +
                $"Rom版本: {account.lastRomVersion}\n" +
                $"Data版本: {account.lastDataVersion}\n" +
                $"DX主要版本: {account.lastGameId}\n" +
                $"最后同步日期: {account.lastUpdate.ToString("yyyy-MM-dd HH:mm:ss")}");

            var ranking = await database.GetUserRank(account.playerRating);

            msg.Edit(
                "用户信息:\n" +
                $"名称: {account.userName}\n" +
                $"Rating: {account.playerRating}\n" +
                $"排名: {ranking}\n" +
                $"Rom版本: {account.lastRomVersion}\n" +
                $"Data版本: {account.lastDataVersion}\n" +
                $"DX主要版本: {account.lastGameId}\n" +
                $"最后同步日期: {account.lastUpdate.ToString("yyyy-MM-dd HH:mm:ss")}");
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, e.ToString());
        }


    }
    /// <summary>
    /// 获取登录地
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal static void GetUserRegion(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        var request = new Request<UserRegionRequest>();
        int userId;
        if (!param.IsEmpty())
            if (!querier.CheckPermission(Permission.Admin))
            {
                userMsg.Send("Permission Denied");
                return;
            }
            else if (int.TryParse(param.First(), out userId))
                request.Object.userId = userId;
            else
            {
                userMsg.Send("参数无效喵x");
                return;
            }
        else
            request.Object.userId = (int)querier.MaiUserId;

        var response = Aqua.Post<UserRegionRequest, UserRegionResponse>(request).Object;
        string regionStr = "";
        int totalPlayCount = 0;
        DateTime firstRegionDate = DateTime.Now;

        if (response.StatusCode is not HttpStatusCode.OK)
        {
            userMsg.Send("获取出勤地区数据失败QAQ\n" +
                       $"对端响应: {response.StatusCode}");
            return;
        }
        if (response.userRegionList.Length == 0)
        {
            userMsg.Send("你看起来从未出过勤呢~");
            return;
        }
        foreach (var region in response.userRegionList)
        {
            regionStr += $"\n\\- *{GetRegionName(region.RegionId)} *\n" +
            StringHandle($"   最早出勤于:{region.CreateDate.ToString("yyyy/MM/dd")}\n" +
                         $"   出勤次数: {region.PlayCount}\n");

            totalPlayCount += region.PlayCount;
            if (region.CreateDate.Ticks < firstRegionDate.Ticks)
                firstRegionDate = region.CreateDate;
        }
        userMsg.Send("你的出勤数据如下:\n" + regionStr +
                    $"\n你最早在{firstRegionDate.ToString("yyyy/MM/dd")}出勤；在过去的{(DateTime.Now - firstRegionDate).Days}天里，你一共出勤了{totalPlayCount}次", ParseMode.MarkdownV2);
    }
    /// <summary>
    /// 绑定maimai账号
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void BindUser(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        try
        {
            int? maiUserId = null;
            var filePath = Path.Combine(Config.TempPath, $"{GetRandomStr()}".Replace("\\", "").Replace("/", ""));

            if (userMsg.IsGroup)
            {
                userMsg.Send("请PM我~");
                return;
            }
            var msg = await userMsg.Send("已收到请求，请耐心等待处理~");

            await Task.Delay(500);
            if (param.IsEmpty())
            {
                await msg.Edit("Invaild params");
                return;
            }
            else if (querier.MaiUserId is not null)
            {
                await msg.Edit("不能重复绑定账号喵x");
                return;
            }

            if (param.First().ToLower() == "image")
            {
                if (userMsg.Photo is null)
                {
                    await msg.Edit("图片喵?");
                    return;
                }

                await msg.Edit("正在下载图片...");

                if (await userMsg.GetPhoto(filePath))
                {
                    await msg.Edit("图片下载完成");
                    await Task.Delay(500);
                    await msg.Edit("正在解析二维码...");
                    await Task.Delay(500);

                    var request = new QRCodeRequest()
                    {
                        KeyChip = Config.keyChips[0],
                        QrCode = Image.FromFile(filePath)
                    };

                    maiUserId = QRCode.ToUserId(request).Object.userID;
                }
                else
                {
                    await msg.Edit("绑定失败，图片下载失败QAQ");
                    return;
                }
            }
            else if (QRCode.IsWeChatId(param.First()))
            {
                var request = new QRCodeRequest()
                {
                    KeyChip = Config.keyChips[0],
                    QrCode = param.First()
                };
                maiUserId = QRCode.ToUserId(request).Object.userID;
            }
            else
            {
                await userMsg.Edit("获取UserId时发送错误QAQ:\nWeChatID无效");
                return;
            }

            if (maiUserId == -1)
            {
                await userMsg.Edit("你的二维码看上去已经过期了呢，请重新获取喵x");
                return;
            }

            await userMsg.Edit("正在获取用户信息...");

            var response = GetUserPreview((int)maiUserId).Result.Object;
            querier.MaiUserId = maiUserId;
            database.GetMaiAccount(querier);
            if (response.StatusCode is not HttpStatusCode.OK)
            {
                await userMsg.Edit("绑定成功，但无法获取用户信息QAQ");
                return;
            }

            await userMsg.Edit(
                "绑定成功\\!\n\n" +
                "用户信息:\n" + StringHandle(
                $"名称: {response.userName}\n" +
                $"Rating: {response.playerRating}\n" +
                $"最后游玩日期: {response.lastPlayDate}"), ParseMode.MarkdownV2);

            Config.SaveData();
            File.Delete(filePath);
        }
        catch
        {
            userMsg.Send("Internal error");
        }
    }
    /* internal static async void UserLogin(Message userMsg)
    {
        //var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, Config.keyChips[0], a => { });
        return;
    } */
    /// <summary>
    /// 备份用户数据
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal static async void DataBackup(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        int userid = (int)querier.MaiUserId;
        string password = "";

        if (param.Length < 1)
        {
            await userMsg.Reply("Invaild params");
            return;
        }
        if (param.Length == 2)
        {
            if (!querier.CheckPermission(Permission.Admin))
            {
                await userMsg.Reply("Permission denied");
                return;
            }
            if (!int.TryParse(param.First(), out userid))
            {
                await userMsg.Reply("缺少参数喵x");
                return;
            }
            password = param[1];
        }
        else
            password = param[0];


        var msg = await userMsg.Reply("已收到请求，请耐心等待处理~");

        await msg.Edit("正在尝试登录... (0/15)");
        try
        {
            var user = await AquaTools.Users.User.Login(userid, Config.keyChips[0], async a => await msg.Edit($"正在获取数据... ({a}/15)"));
            await msg.Edit("获取数据成功,正在上传备份文件...");
            var userdata = user.Export(password);
            var stream = new MemoryStream(userdata);
            //await UploadFile(stream, $"UserDataBackup{DateTime.Now.ToString("yyyyMMddhhmm")}.data", update.Message.Chat.Id);
            await msg.Edit("数据备份完成喵x");
            user.Logout();
        }
        catch (LoginFailureException e)
        {

            msg.Edit("登录失败,请检查二维码是否过期QAQ\n" +
                $"```csharp\n" +
                $"{StringHandle($"{e.Message}")}\n" +
                $"```", ParseMode.MarkdownV2);

        }
        catch (Exception e)
        {
            msg.Edit($"出现未知错误QAQ\n" +
                $"```csharp\n" +
                $"{StringHandle($"{e.Message}")}\n" +
                $"```", ParseMode.MarkdownV2);
        }
        finally
        {
            AquaTools.Users.User.Logout((int)querier.MaiUserId);
        }
    }
    /// <summary>
    /// 强制更新MaiAccount数据
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void UpdateUserData(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        var msg = await userMsg.Reply("已收到请求，请耐心等待处理~");
        int userId;
        if (!param.IsEmpty())
        {
            if (!querier.CheckPermission(Permission.Admin))
            {
                await msg.Edit("Access denied");
                return;
            }
            else if (!int.TryParse(param.First(), out userId))
            {
                await msg.Edit("Invaild params");
                return;
            }
        }
        else if (param.Length > 1)
        {
            await msg.Edit("Invaild params");
            return;
        }
        else
            userId = (int)querier.MaiUserId;

        try
        {
            var maiUser = database.Search(userId);
            bool isNew = maiUser == null;
            var response = (await GetUserPreview(userId)).Object;

            if (response.StatusCode is HttpStatusCode.OK)
            {
                if (maiUser is null)
                    maiUser = new MaiAccount();
                maiUser.playerRating = response.playerRating ?? 0;
                maiUser.lastDataVersion = response.lastDataVersion;
                maiUser.lastRomVersion = response.lastRomVersion;
                maiUser.lastGameId = response.lastGameId;
                maiUser.banState = response.banState;
                maiUser.lastUpdate = DateTime.Now;

                querier.Account = maiUser;
                if (isNew)
                    database.MaiAccountList.Add(maiUser);
                Config.SaveData();

                await msg.Edit("更新完成喵wAw");
            }
            else
                throw new Exception("");
        }
        catch (Exception e)
        {
            await msg.Edit("发生了未知错误QAQ\n" +
                "```csharp\n" +
                $"{e.Message}\n" +
                $"```", ParseMode.MarkdownV2);
        }
    }
    /* internal static async void GetTicket(Message userMsg)
    {
        int count = 1;
        int ticketType = 0;
        if (command.Params.Length == 0)
        {
            //GetHelpInfo(userMsg);
            return;
        }

        var selfMessage = await userMsg.Send("已收到请求，请耐心等待处理~", update);
        Dictionary<string, int> vaildTicketType = new()
    {
        { "2",2 } ,
        { "3",3 } ,
        { "5",5 } ,
        { "20",20020 } ,
    };


        if (command.Params.Length < 3)
        {
            if (!vaildTicketType.ContainsKey(command.Params[0]))
            {
                EditMessage("参数错误喵x", update, selfMessage.MessageId);
                return;
            }
            else if (command.Params.Length == 2 && !int.TryParse(command.Params[1], out count))
            {
                EditMessage("参数错误喵x", update, selfMessage.MessageId);
                return;
            }
            ticketType = vaildTicketType[command.Params[0]];
        }
        else
        {
            EditMessage("参数错误喵x", update, selfMessage.MessageId);
            return;
        }



        EditMessage("正在尝试登录... (0/15)", update, selfMessage.MessageId);
        try
        {
            var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, Config.keyChips[0], async a => await EditMessage($"正在获取数据... ({a}/15)", update, selfMessage.MessageId));
            await EditMessage("正在尝试申请跑图券...", update, selfMessage.MessageId);
            var result = user.CreateNewTicket((ChargeType)ticketType, count, DateTime.Now.AddDays(14));
            if (result)
                EditMessage("跑图券获取成功wAw", update, selfMessage.MessageId);
            else
                EditMessage("跑图券获取失败，请检查你是否已有相同的券QAQ", update, selfMessage.MessageId);
        }
        catch (Exception e)
        {
            EditMessage($"出现未知错误QAQ\n" +
                $"```csharp\n" +
                $"{StringHandle($"{e.Message}")}\n" +
                $"```", update, selfMessage.MessageId, ParseMode.MarkdownV2);
        }
        finally
        {
            AquaTools.Users.User.Logout((int)querier.MaiUserId);
        }
    } */
    /* internal static async void Upsert(Message userMsg)
    {
        var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, Config.keyChips[0], a => { });
        var playlogs = new List<UserPlaylog>();
        var musicDetail = user.CreatePlaylog(11422, new Dictionary<string, int>
    {
        { "Achievement" , 1008750 },
        { "ComboStatus" , 3 },
        { "SyncStatus" , 0 },
        { "DeluxscoreMax" , 1815 },
        { "ScoreRank" , 13 },
    }, MusicLevelType.Master, false);

        NoteInfo[] noteInfo =
        {
        new NoteInfo
            {
                CriticalPerfect = 382,
                Perfect = 0,
                Fast = 0,
                Late = 0,
                Good = 0,
                Great = 0,
                Miss = 0
            },
        new NoteInfo
            {
                CriticalPerfect = 38,
                Perfect = 0,
                Fast = 0,
                Late = 0,
                Good = 0,
                Great = 0,
                Miss = 0
            },
        new NoteInfo
            {
                CriticalPerfect = 135,
                Perfect = 0,
                Fast = 0,
                Late = 0,
                Good = 0,
                Great = 0,
                Miss = 0
            },
        new NoteInfo
            {
                CriticalPerfect = 44,
                Perfect = 0,
                Fast = 0,
                Late = 0,
                Good = 0,
                Great = 0,
                Miss = 0
            },
        new NoteInfo
            {
                CriticalPerfect = 4,
                Perfect = 2,
                Fast = 2,
                Late = 0,
                Good = 0,
                Great = 0,
                Miss = 0
            }
    };

        playlogs.Add(user.CreateUserPlaylog(musicDetail,
            new Dictionary<string, int>()
            {
            { "isRandom" , 0},
            { "MaxCombo" , 605}
            },
            noteInfo, null, (long)user.LoginId, 1));
        playlogs.Add(user.CreateUserPlaylog(musicDetail,
            new Dictionary<string, int>()
            {
            { "isRandom" , 0},
            { "MaxCombo" , 605}
            },
            noteInfo, null, (long)user.LoginId, 2));
        playlogs.Add(user.CreateUserPlaylog(musicDetail,
            new Dictionary<string, int>()
            {
            { "isRandom" , 0},
            { "MaxCombo" , 605}
            },
            noteInfo, null, (long)user.LoginId, 3));

        var result = user.UpsertAll(playlogs.ToArray(), (long)user.LoginId);
        user.Logout();
        return;
    } */
    /// <summary>
    /// 逃离小黑屋
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void Logout(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();
        var group = userMsg.GetGroup();

        var userId = (int)querier.MaiUserId;
        if (!param.IsEmpty())
        {
            if (!querier.CheckPermission(Permission.Admin, group))
            {
                //GetHelpInfo(userMsg);
                return;
            }
            else
                int.TryParse(param.First(),out userId);
        }

        var request = new Request<UserLogoutRequest>(new UserLogoutRequest() { userId = userId });

        var result = await Aqua.PostAsync<UserLogoutRequest, UserLogoutResponse>(request);

        if (result is not null)
            await userMsg.Reply("已发信，请检查是否生效~\n" +
                       $"对端响应: {result.Object.StatusCode}");
        else
            await userMsg.Reply("发信失败QAQ\n" +
                       $"对端响应: {result.Object.StatusCode}");

    }
    /// <summary>
    /// 获取国服排行榜
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal void GetTopRank(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        if (!param.IsEmpty())
        {
            if (param.First() == "refresh")
            {
                database.CalRating();
                userMsg.Reply("Mai rank have been updated ~");
                return;
            }
            else
            {
                //GetHelpInfo(userMsg);
                return;
            }
        }

        var rank = database.Top.Select(x => x.ToList()).ToList();
        var strHeader = "全国前300排行榜\n" +
                        "```markdown\n" +
                        $"{"名次".PadRight(14)}{"Rating".PadRight(16)}{"名称".PadRight(12)}\n";
        var strFooter = "```";
        int ranking = 1;
        int count = 0;
        int index = 1;
        var playerInfoStr = "";
        foreach (var playerGroup in rank)
        {
            foreach (var player in playerGroup)
            {
                playerInfoStr += StringHandle($"{ranking.ToString().PadRight(14)}{player.playerRating.ToString().PadRight(16)}{player.userName.PadRight(12)}\n");
                count++;
                if (count == 50)
                {
                    userMsg.Reply(strHeader + playerInfoStr + strFooter + $"\n\\({index}\\/6\\)", ParseMode.MarkdownV2);
                    index++;
                    count = 0;
                    playerInfoStr = "";
                    Thread.Sleep(800);
                }
            }
            ranking += playerGroup.Count;
        }
    }
    /// <summary>
    /// 获取Mai土豆服务器状态
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void GetServerStatus(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();
        if (cmd.Prefix == "maistatus")
            param = cmd.Params;

        var titlePingInfo = monitor.GetAvgPing(ServerType.Title);
        var oauthPingInfo = monitor.GetAvgPing(ServerType.OAuth);
        var netPingInfo = monitor.GetAvgPing(ServerType.Net);
        var mainPingInfo = monitor.GetAvgPing(ServerType.Main);
        var skipRateInfo = monitor.GetAvgSkipRate();
        string text = "";
        if (param.IsEmpty())
        {
            text = "maimai服务器状态:\n" +
                      "```python" +
                     StringHandle(
                      "\nTcping延迟:" +
                     $"\n  - Title服务器  : {monitor.TitleServerDelay}ms" +
                     $"\n  - OAuth服务器  : {monitor.OAuthServerDelay}ms" +
                     $"\n  - DXNet服务器  : {monitor.NetServerDelay}ms" +
                     $"\n  - Main 服务器  : {monitor.MainServerDelay}ms" +
                     $"\n" +
                     $"响应包跳过率 : \n" +
                     $"  - 30min  : {Math.Round(skipRateInfo[0] * 100, 2)}%\n" +
                     $"  - 60min  : {Math.Round(skipRateInfo[1] * 100, 2)}%\n" +
                     $"  - 90min  : {Math.Round(skipRateInfo[2] * 100, 2)}%\n" +
                     $"\n") +
                      "```";
        }
        else if (param.First() is "full")
        {
            text = "maimai服务器状态:\n" +
                      "```python" +
                     StringHandle(
                      "\nTcping延迟:" +
                     $"\n- Title服务器  : {monitor.TitleServerDelay}ms\n" +
                     $"  -  5min  : {titlePingInfo[0]}ms\n" +
                     $"  - 10min  : {titlePingInfo[1]}ms\n" +
                     $"  - 15min  : {titlePingInfo[2]}ms" +
                     $"\n- OAuth服务器  : {monitor.OAuthServerDelay}ms\n" +
                     $"  -  5min  : {oauthPingInfo[0]}ms\n" +
                     $"  - 10min  : {oauthPingInfo[1]}ms\n" +
                     $"  - 15min  : {oauthPingInfo[2]}ms" +
                     $"\n- DXNet服务器  : {monitor.NetServerDelay}ms\n" +
                     $"  -  5min  : {netPingInfo[0]}ms\n" +
                     $"  - 10min  : {netPingInfo[1]}ms\n" +
                     $"  - 15min  : {netPingInfo[2]}ms" +
                     $"\n- Main 服务器  : {monitor.MainServerDelay}ms\n" +
                     $"  -  5min  : {mainPingInfo[0]}ms\n" +
                     $"  - 10min  : {mainPingInfo[1]}ms\n" +
                     $"  - 15min  : {mainPingInfo[2]}ms" +
                     $"\n\n" +
                      "响应状态:\n" +
                     $"- 发送包数累计 : {monitor.TotalRequestCount}\n" +
                     $"- 响应超时累计 : {monitor.TimeoutRequestCount}\n" +
                     $"- 其他错误累计 : {monitor.OtherErrorCount}\n" +
                     $"- 非压缩包累计 : {monitor.CompressSkipRequestCount}\n" +
                     $"- 响应包跳过率 : \n" +
                     $"  -  5min  : {Math.Round(skipRateInfo[0] * 100, 2)}%\n" +
                     $"  - 10min  : {Math.Round(skipRateInfo[1] * 100, 2)}%\n" +
                     $"  - 15min  : {Math.Round(skipRateInfo[2] * 100, 2)}%\n" +
                     $"  -  Avg   : {Math.Round(monitor.CompressSkipRate * 100, 2)}%\n" +
                     $"- 最新一次响应 : {monitor.LastResponseStatusCode}\n\n" +
                     $"\n") +
                      "```";
        }
        else
            text = $"\"{string.Join(" ", param)}\"为无效参数喵x";

        await userMsg.Reply(text,ParseMode.MarkdownV2);
    }
    /// <summary>
    /// 获取RegionId对应的地区名
    /// </summary>
    /// <param name="regionId"></param>
    /// <returns></returns>
    internal static string GetRegionName(int regionId)
    {
        return regionId switch
        {
            1 => "北京",
            2 => "重庆",
            3 => "上海",
            4 => "天津",
            5 => "安徽",
            6 => "福建",
            7 => "甘肃",
            8 => "广东",
            9 => "贵州",
            10 => "海南",
            11 => "河北",
            12 => "黑龙江",
            13 => "河南",
            14 => "湖北",
            15 => "湖南",
            16 => "江苏",
            17 => "江西",
            18 => "吉林",
            19 => "辽宁",
            20 => "青海",
            21 => "陕西",
            22 => "山东",
            23 => "山西",
            24 => "四川",
            25 => "台湾",
            26 => "云南",
            27 => "浙江",
            28 => "广西",
            29 => "内蒙古",
            30 => "宁夏",
            31 => "新疆",
            32 => "西藏",
            _ => null
        };
    }
    internal static async Task<Response<UserPreviewResponse>> GetUserPreview(int userId)
    {
        var request = new Request<UserPreviewRequest>();
        request.Object.userId = userId;

        return await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request);
    }
    static string GetRandomStr() => Convert.ToBase64String(SHA512.HashData(Guid.NewGuid().ToByteArray()));
    async void GetHelpInfo(Command cmd,Message userMsg)
    {

        string helpStr = "```python\n";
        switch (cmd.Prefix)
        {
            case "mai":
                helpStr += Core.StringHandle(
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
            case "maiscanner":
                helpStr += Core.StringHandle(
                        "命令用法：\n" +
                        "\n/maiscanner status       获取扫描器状态" +
                        "\n/maiscanner update [int] 从指定位置更新数据库" +
                        "\n/maiscanner update       更新数据库" +
                        "\n/maiscanner stop         终止当前任务" +
                        "\n/maiscanner set [int]    设置QPS限制");
                break;
            default:
                userMsg.Reply("该命令暂未添加说明信息喵x");
                return;
        }
        helpStr += "\n```";
        await userMsg.Reply(helpStr,ParseMode.MarkdownV2);
    }
}
public partial class Mai
{
    public enum ServerType
    {
        Title,
        OAuth,
        Net,
        Main
    }
    public class PingResult
    {
        public ServerType Type { get; set; }
        public long Delay { get; set; }
    }
    public class SkipLog
    {
        public DateTime Timestamp { get; set; }
        public bool IsSkip { get; set; }
        public double LastSkipRate { get; set; }
    }
    public struct DateTimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Contains(DateTime dt) => (dt - Start).Seconds >= 0 && (dt - End).Seconds < 0;
        public static DateTimeRange[] Create(int minute)
        {
            var count = 1440 / minute;
            var range = new List<DateTimeRange>();

            var day = DateTime.Today;

            for (var i = 0; i < count; i++)
                range.Add(new DateTimeRange()
                {
                    Start = day,
                    End = day.AddMinutes(minute)
                });
            return range.ToArray();
        }
    }
    internal class MaiDatabase
    {
        public List<MaiAccount> MaiAccountList = new();
        public List<int> MaiInvaildUserIdList = new();
        public List<long> RatingList = new();
        public List<IGrouping<long, MaiAccount>> Top = new();

        public void Init()
        {
            MaiAccountList = Load<List<MaiAccount>>(Path.Combine(DatabasePath, "MaiAccountList.data"));
            MaiInvaildUserIdList = Load<List<int>>(Path.Combine(DatabasePath, "MaiInvalidUserIdList.data"));

            foreach (var user in TUserList)
                GetMaiAccount(user);
            CalRating();
        }
        public async void GetMaiAccount(TelegramBot.Types.User user)
        {
            await Task.Run(() =>
            {
                if (user.MaiUserId is null)
                    return false;

                var userid = (int)user.MaiUserId;
                var result = MaiAccountList.Where(x => x.userId == userid);

                if (result.Count() == 0)
                    return false;

                user.Account = result.ToArray()[0];
                return true;
            });
        }
        public void Save()
        {
            Config.Save(Path.Combine(DatabasePath, "MaiAccountList.data"), MaiAccountList);
            Config.Save(Path.Combine(DatabasePath, "MaiInvalidUserIdList.data"), MaiInvaildUserIdList);
        }
        public void CalRating()
        {
            var allRating = MaiAccountList.OrderBy(x => x.playerRating);
            RatingList = allRating.OrderByDescending(x => x.playerRating).Select(x => x.playerRating).ToList();
            var top = allRating.Skip(allRating.Count() - 300).OrderByDescending(x => x.playerRating);
            var ratingGroup = top.GroupBy(x => x.playerRating);

            Top = ratingGroup.ToList();
        }
        public async Task<long> GetUserRank(long rating)
        {
            return await Task.Run(() =>
            {
                var rankList = RatingList.GroupBy(x => x);
                int ranking = 1;
                foreach (var rankGroup in rankList)
                {
                    if (rankGroup.Key == rating)
                        return ranking;
                    ranking += rankGroup.Count();
                }
                return -1;
            });
        }
        public MaiAccount Search(int userId)
        {
            var result = MaiAccountList.Where(x => x.userId == userId).ToArray();
            if (result.Length == 0)
                return null;
            else
                return result[0];
        }
    }
    public partial class MaiMonitor
    {

        public bool ServiceAvailability = true;

        public long FaultInterval = -1;//平均故障间隔
        static List<long> FaultIntervalList = new();
        static DateTime LastFailureTime;

        public long TitleServerDelay = -1;// Title Server
        public long OAuthServerDelay = -1;// WeChat QRCode
        public long NetServerDelay = -1;//   DX Net
        public long MainServerDelay = -1;//  Main Server

        public List<PingResult> PingLogs = new();

        public long TotalRequestCount = 0;
        public long TimeoutRequestCount = 0;
        public long OtherErrorCount = 0;
        public double CompressSkipRate = 0;// 跳过率
        public long CompressSkipRequestCount = 0;
        public List<SkipLog> CompressSkipLogs = new();
        public HttpStatusCode LastResponseStatusCode;

        static Mutex mutex = new Mutex();
        Task monitorTask;
        bool isDestroying = false;

        public void Init()
        {
            LastFailureTime = DateTime.Now;
            //CompressSkipLogs = Load<List<SkipLog>>(Path.Combine(DatabasePath, "CompressSkipLogs.data"));
            FaultIntervalList = Load<List<long>>(Path.Combine(Config.DatabasePath, "FaultIntervalList.data"));
            LastFailureTime = Load<DateTime>(Path.Combine(Config.DatabasePath, "LastFailureTime.data"));


            Proc();
        }
        public void Save()
        {
            //Config.Save(Path.Combine(DatabasePath, "CompressSkipLogs.data"), CompressSkipLogs);
            //Config.Save(Path.Combine(Config.DatabasePath, "FaultIntervalList.data"),FaultIntervalList);
            //Config.Save(Path.Combine(Config.DatabasePath, "LastFailureTime.data"), LastFailureTime);
        }
        public void Destroy()
        {
            isDestroying = true;
            if (monitorTask is not null)
                monitorTask.Wait();
        }
        void Proc()
        {
            monitorTask = Task.Run(() =>
            {
                while (true)
                {
                    if (isDestroying)
                        break;
                    if (DateTime.Today.AddHours(4) <= DateTime.Now && DateTime.Now <= DateTime.Today.AddHours(9))
                    {
                        TitleServerDelay = -1;
                        OAuthServerDelay = -1;
                        NetServerDelay = -1;
                        MainServerDelay = -1;
                        TotalRequestCount = 0;
                        TimeoutRequestCount = 0;
                        CompressSkipRate = 0;
                        OtherErrorCount = 0;
                        CompressSkipRequestCount = 0;
                        //CompressSkipLogs.Clear();
                        PingLogs.Clear();
                        continue;
                    }

                    //var response = Aqua.TestPostAsync(Config.keyChips[0]).Result;
                    var req = new Request<UserRegionRequest>();
                    req.Object.userId = 11015484;
                    var response = Aqua.Post<UserRegionRequest, BaseResponse>(req);
                    LastResponseStatusCode = response.Object.StatusCode;
                    TotalRequestCount++;
                    Task.Run(() =>
                    {
                        mutex.WaitOne();
                        TitleServerDelay = TCPing(MaiServer.URL.Title, 42081);
                        OAuthServerDelay = TCPing(MaiServer.URL.OAuth, 443);
                        NetServerDelay = TCPing(MaiServer.URL.Net, 443);
                        MainServerDelay = TCPing(MaiServer.URL.Main, 80);


                        PingLogs.Add(new PingResult() { Type = ServerType.Title, Delay = TitleServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.OAuth, Delay = OAuthServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Net, Delay = NetServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Main, Delay = MainServerDelay });
                        mutex.ReleaseMutex();
                    });

                    if (TitleServerDelay == -1 || OAuthServerDelay == -1 || NetServerDelay == -1 || MainServerDelay == -1)
                    {
                        if (ServiceAvailability)
                            FaultIntervalList.Add((long)(LastFailureTime - DateTime.Now).TotalSeconds);
                        ServiceAvailability = false;
                    }
                    else
                    {
                        if (!ServiceAvailability)
                            LastFailureTime = DateTime.Now;
                        ServiceAvailability = true;
                    }
                    var lastSkip = GetAvgSkipRate()[0];
                    if (LastResponseStatusCode == HttpStatusCode.GatewayTimeout)
                        TimeoutRequestCount++;
                    else if (LastResponseStatusCode is HttpStatusCode.OK)
                    {
                        CompressSkipRequestCount++;
                        CompressSkipLogs.Add(new SkipLog()
                        {
                            Timestamp = DateTime.Now,
                            IsSkip = response.Object.CompressSkip,
                            LastSkipRate = lastSkip
                        });
                    }
                    else
                        OtherErrorCount++;

                    CompressSkipRate = (double)CompressSkipRequestCount / (TotalRequestCount - TimeoutRequestCount - OtherErrorCount);
                    if (FaultIntervalList.Count != 0)
                        FaultInterval = FaultIntervalList.Sum() / FaultIntervalList.Count();

                    CompressSkipLogs = CompressSkipLogs.Where(x => (DateTime.Now - x.Timestamp).Minutes <= 90).ToList();


                    Config.Save(Path.Combine(Config.DatabasePath, "FaultIntervalList.data"), FaultIntervalList, false);
                    Config.Save(Path.Combine(Config.DatabasePath, "LastFailureTime.data"), LastFailureTime, false);
                    //Config.Save(Path.Combine(DatabasePath, "CompressSkipLogs.data"), CompressSkipLogs, false);


                    Thread.Sleep(5000);
                }
            });
        }

        public long[] GetAvgPing(ServerType type)
        {
            long _5min = -1;
            long _10min = -1;
            long _15min = -1;

            var result = PingLogs.Where(x => x.Type == type).Select(x => x.Delay);
            var count = result.Count();
            if (count >= 60)
                _5min = result.Skip(Math.Max(0, count - 60)).Sum() / 60;
            if (count >= 120)
                _10min = result.Skip(Math.Max(0, count - 120)).Sum() / 120;
            if (count >= 180)
                _15min = result.Skip(Math.Max(0, count - 180)).Sum() / 180;

            return new long[] { _5min, _10min, _15min };
        }
        public double[] GetAvgSkipRate()
        {
            double _30min = double.NaN;
            double _60min = double.NaN;
            double _90min = double.NaN;
            var count = CompressSkipLogs.Count;
            var now = DateTime.Now;

            var _30minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 30);
            var _60minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 60);
            var _90minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 90);

            var _30minSkip = _30minLogs.Where(x => x.IsSkip);
            var _60minSkip = _60minLogs.Where(x => x.IsSkip);
            var _90minSkip = _90minLogs.Where(x => x.IsSkip);

            _30min = _30minSkip.Count() / (double)_30minLogs.Count();
            _60min = _60minSkip.Count() / (double)_60minLogs.Count();
            _90min = _90minSkip.Count() / (double)_90minLogs.Count();

            //if (count >= 60)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 60))
            //                                       .Where(x => x.IsSkip && (now - x.Timestamp).Minutes <= 5)
            //                                       .Count();
            //    _5min = skipCount / 60;
            //}
            //if (count >= 120)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 120))
            //                                       .Where(x => x.IsSkip)
            //                                       .Count();
            //    _10min = skipCount / 120;
            //}
            //if (count >= 180)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 180))
            //                                       .Where(x => x.IsSkip)
            //                                       .Count();
            //    _15min = skipCount / 180;
            //}

            return new double[] { _30min, _60min, _90min };
        }
        static long TCPing(string host, int port)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            try
            {
                TcpClient client = new TcpClient();
                client.SendTimeout = 2000;
                client.ReceiveTimeout = 2000;
                client.Connect(host, port);
                client.Close();
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return -1;
            }
            finally
            {
                stopwatch.Stop();
            }

        }

    }
    public partial class MaiMonitor
    {
        /// <summary>
        /// 用于获取指定尺度的全天K线图
        /// </summary>
        /// <param name="minute"></param>
        public void CreateGraph(int minute)
        {
            var range = DateTimeRange.Create(minute);
        }
        public void CreateGraph(DateTimeRange[] range)
        {
            var xSamples = range.Select(x => x.Start.ToString("HH:mm")).ToArray();
            List<KNode> nodes = new();

            foreach (var item in range)
                nodes.Add(CreateNode(item, CompressSkipLogs.ToArray()));

            var ySamples = CreateYSamples(nodes);
        }
        KNode CreateNode(DateTimeRange range, SkipLog[] samples)
        {
            double max = 0;
            double min = 1;
            double open = 0;
            double close = 0;

            var matched = samples.Where(x => range.Contains(x.Timestamp))
                                 .OrderBy(x => x.Timestamp);

            if (matched.Count() == 0)
                return new KNode()
                {
                    High = 0,
                    Low = 0,
                    Close = 0,
                    Open = 0,
                    Date = range.Start
                };

            foreach (var x in matched)
            {
                max = Math.Max(x.LastSkipRate, max);
                min = Math.Min(x.LastSkipRate, min);
            }
            open = matched.First().LastSkipRate;
            close = matched.Last().LastSkipRate;

            return new KNode()
            {
                High = (float)max,
                Low = (float)min,
                Open = (float)open,
                Close = (float)close,
                Date = range.Start
            };


        }
        IList<float> CreateYSamples(IList<KNode> nodes)
        {
            List<float> samples = new();
            var maxValue = (((int)(nodes.Select(x => x.High).Max() * 100) / 5) + 1) * 5 / 100f;

            for (float i = maxValue; i >= 0;)
            {
                samples.Add(i);
                i -= 0.05f;
            }

            return samples;
        }
    }
    public partial class MaiScanner
    {

        static string AppPath = Environment.CurrentDirectory;
        static string DatabasePath = Path.Combine(AppPath, "Database");
        static string TempPath = Path.Combine(AppPath, "Database/ScanTempFile");


        public int StartIndex = 1000;
        public int EndIndex = 1199;
        public int SearchInterval = 1000;
        public int CurrentQps = 0;
        public int QpsLimit = 10;

        public long TotalAccountCount = 0;
        public long CurrentAccountCount = 0;

        public bool isRunning = false;

        List<Task> task = new();
        Mutex mutex = new();
        Mutex mutex2 = new();
        Task QpsTimer = null;
        bool isDestroying = false;
        public CancellationTokenSource cancelSource = new CancellationTokenSource();
        public void Init()
        {

        }
        public void Destroy()
        {
            isRunning = false;
            isDestroying = true;

            if (task.Count > 0)
                Task.WaitAll(task.ToArray());
            if (QpsTimer is not null)
                QpsTimer.Wait();
        }
        public async void Start()
        {
            if (QpsTimer is null)
            {
                QpsTimer = Task.Run(() =>
                {
                    while (true)
                    {
                        if (isDestroying)
                            break;
                        mutex.WaitOne();
                        CurrentQps = 0;
                        mutex.ReleaseMutex();
                        Thread.Sleep(1000);
                    }
                });
            }

            if (isRunning)
                return;

            Aqua.RePostCount = 10;
            Aqua.Timeout = 1000;
            isRunning = true;
            task.Clear();
            cancelSource = new CancellationTokenSource();

            TotalAccountCount = (EndIndex - StartIndex + 1) * 10000;

            await Task.Run(() =>
            {
                for (; StartIndex <= EndIndex; StartIndex++)
                    task.Add(GetUser(StartIndex * 10000, StartIndex * 10000 + 9999));
                Task.WaitAll(task.ToArray());
                IsFinished();
                Config.SaveData();
            });
        }
        public async void Update(int index = 0)
        {
            if (isRunning)
                return;

            Aqua.RePostCount = 10;
            Aqua.Timeout = 1000;
            isRunning = true;
            task.Clear();
            cancelSource = new CancellationTokenSource();

            TotalAccountCount = database.MaiAccountList.Count;
            CurrentAccountCount = index;

            await Task.Run(() =>
            {
                var accounts = database.MaiAccountList;
                for (; index < accounts.Count; index++)
                    task.Add(UpdateUser(accounts[index]));
                Task.WaitAll(task.ToArray());
                IsFinished();
                Config.SaveData();
            });
        }
        async Task UpdateUser(MaiAccount account)
        {
            bool canUpdate = true;
            while (true)
            {
                if (isDestroying)
                    break;
                if (DateTime.Today.AddHours(3) <= DateTime.Now && DateTime.Now <= DateTime.Today.AddHours(9))
                    canUpdate = false;
                else
                    canUpdate = true;

                var request = new Request<UserPreviewRequest>();
                request.Object.userId = account.userId;

                QpsIncrease();
                for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20 || !canUpdate; QpsIncrease())
                {
                    if (cancelSource.Token.IsCancellationRequested)
                        break;
                    Thread.Sleep(500);
                }
                if (cancelSource.Token.IsCancellationRequested)
                    break;


                var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    continue;

                account.playerRating = response.playerRating ?? 0;
                account.lastDataVersion = response.lastDataVersion;
                account.lastRomVersion = response.lastRomVersion;
                account.lastGameId = response.lastGameId;
                account.banState = response.banState;
                account.lastUpdate = DateTime.Now;
                CountIncrease();
                break;

            }

        }
        async Task GetUser(int startIndex, int endIndex)
        {
            int targetUserId = startIndex;
            List<MaiAccount> accounts = new();
            List<int> failureList = new();

            for (; targetUserId <= endIndex; targetUserId++)
            {
                if (isDestroying)
                    break;
                for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20;)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var request = new Request<UserPreviewRequest>();
                request.Object.userId = targetUserId;

                var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                if (response.StatusCode is not HttpStatusCode.OK)
                {
                    failureList.Add(targetUserId);
                    continue;
                }

                var account = new MaiAccount();
                account.userName = StringHandle(response.userName);
                account.playerRating = response.playerRating ?? 0;
                account.userId = targetUserId;
                account.lastDataVersion = response.lastDataVersion;
                account.lastRomVersion = response.lastRomVersion;
                account.lastGameId = response.lastGameId;
                account.banState = response.banState;
                account.lastUpdate = DateTime.Now;
                accounts.Add(account);
                CountIncrease();

                QpsIncrease();
                Thread.Sleep(SearchInterval);

                if (!isRunning)
                    return;
            }

            for (; failureList.Count != 0;)
            {
                if (isDestroying)
                    break;
                var _failureList = new List<int>(failureList);
                foreach (var userid in _failureList)
                {
                    for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20;)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    failureList.Remove(userid);
                    var request = new Request<UserPreviewRequest>();
                    request.Object.userId = userid;

                    var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                    if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    {
                        failureList.Add(userid);
                        continue;
                    }

                    var account = new MaiAccount();
                    account.userName = StringHandle(response.userName);
                    account.playerRating = response.playerRating ?? 0;
                    account.userId = userid;
                    account.lastDataVersion = response.lastDataVersion;
                    account.lastRomVersion = response.lastRomVersion;
                    account.lastGameId = response.lastGameId;
                    account.banState = response.banState;
                    account.lastUpdate = DateTime.Now;
                    accounts.Add(account);
                    CountIncrease();

                    QpsIncrease();
                    Thread.Sleep(SearchInterval);

                    if (!isRunning)
                        return;
                }

                continue;
            }

            var result = accounts.GroupBy(x => x.userId)
                                 .Select(x => x.First())
                                 .Where(x => x.userName is not null);

            database.MaiAccountList.AddRange(result);
        }
    }
    public partial class MaiScanner
    {
        /// <summary>
        /// QPS递增
        /// </summary>
        void QpsIncrease()
        {
            mutex.WaitOne();
            CurrentQps++;
            mutex.ReleaseMutex();
        }
        /// <summary>
        /// 用于报告目前进度
        /// </summary>
        void CountIncrease()
        {
            mutex2.WaitOne();
            CurrentAccountCount++;
            mutex2.ReleaseMutex();
        }
        bool IsFinished()
        {
            var count = task.Where(x => x.IsCompleted == true).Count();
            if (count != task.Count)
            {
                isRunning = false;
                return false;
            }
            return true;
        }
        public static string ToJsonString<T>(T target) => Serialize(target);
        public static T FromJsonString<T>(string json) => Deserialize<T>(json);
        public static string Serialize<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeConverter() },
                IncludeFields = true,
                WriteIndented = true
            };
            return JsonSerializer.Serialize(obj, options);
        }
        public static T Deserialize<T>(string json)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeConverter() },
                IncludeFields = true
            };
            var result = JsonSerializer.Deserialize<T>(json, options);

            return result;
        }
    }
    public static string StringHandle(string s)
    {
        if (s is null)
            return null;

        StringBuilder sb = new();
        foreach (char c in s)
        {
            if (c == '　')
                sb.Append(' ');
            else if (c >= 0xFF01 && c <= 0xFF5E)
                sb.Append((char)(c - 0xFEE0));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}