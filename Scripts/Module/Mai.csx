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
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.CodeAnalysis;
using CSScripting;
using NekoBot.Interfaces;
using NekoBot;
using NekoBot.Types;
using Version = NekoBot.Types.Version;
using Message = NekoBot.Types.Message;
using File = System.IO.File;
using MaiAccount = NekoBot.Types.MaiAccount;
using NekoBot.Exceptions;
#pragma warning disable CS4014
public partial class Mai : Destroyable, IExtension , IDestroyable
{
    IDatabase<MaiAccount> maiDatabase
    {
        get
        {
            var newDB = ScriptManager.GetExtension("MaiDatabase");
            if (newDB is not null and IDatabase<MaiAccount> db &&
               db != _maiDatabase)
            {
                _maiDatabase = db;
            }
            return _maiDatabase ?? throw new DatabaseNotFoundException("This script depends on the database");
        }
    }
    IDatabase<MaiAccount>? _maiDatabase;

    List<MaiAccount>? maiAccountList;
    List<long>? ratingList;
    List<IGrouping<long, MaiAccount>>? top;
    List<KeyChip> keyChips { get; set; } = new()
    {
        new KeyChip()
        {
            PlaceId = 2120,
            PlaceName = "SUPER101潮漫北流店",
            RegionId = 28,
            RegionName = "广西",
            KeyChipId = "A63E-01E14596415"
        },
        new KeyChip()
        {
            PlaceId = 1,
            PlaceName = "Unknow",
            RegionId = 1,
            RegionName = "Unknow",
            KeyChipId = "A63E-01E14150010"
        }
    };
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "Mai",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Module,
        Commands = new BotCommand[]
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
                Name = "MaiDatabase",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Database
            },
            new ExtensionInfo()
            {
                Name = "JsonSerializer",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Serializer
            },
            new ExtensionInfo()
            {
                Name = "YamlSerializer",
                Version = new Version() { Major = 1, Minor = 0 },
                Type = ExtensionType.Serializer
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
        _maiDatabase = ((ScriptManager.GetExtension("MaiDatabase") ?? throw new DatabaseNotFoundException("This script depends on the database to initialize"))
                        as IDatabase<MaiAccount>)!;
        _maiDatabase.OnDestroy += () => _maiDatabase = null;

    }
    public override void Destroy()
    {
        _maiDatabase = null;
    }
    public override void Handle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var group = userMsg.Group;

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
            querier.Account ??= maiDatabase.Find(x => x.userId == ((int)querier.Id));
            MaiAccount? account = querier.Account;
            async Task<MaiAccount?> getAccount(int userid)
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

                    maiDatabase.Add(maiAccount);
                    //Config.SaveData();
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

                account = maiDatabase.Find(x => x.userId == id);

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
                    querier.Account = await getAccount((int)querier.MaiUserId!);
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

            var ranking = await GetUserRank(account.playerRating);

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
                        KeyChip = keyChips[0],
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
                    KeyChip = keyChips[0],
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
            querier.Account = maiDatabase.Find(x => x.userId == maiUserId);
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

            //Config.SaveData();
            File.Delete(filePath);
        }
        catch
        {
            userMsg.Send("Internal error");
        }
    }
    /* internal static async void UserLogin(Message userMsg)
    {
        //var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, keyChips[0], a => { });
        return;
    } */
    /// <summary>
    /// 备份用户数据
    /// </summary>
    /// <param name="command"></param>
    /// <param name="update"></param>
    /// <param name="querier"></param>
    internal async void DataBackup(Message userMsg)
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
            var user = await AquaTools.Users.User.Login(userid, keyChips[0], async a => await msg.Edit($"正在获取数据... ({a}/15)"));
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
            var maiUser = maiDatabase.Find(x => x.userId == userId);
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
                    maiDatabase.Update(x=>x.userId == maiUser.userId, maiUser);
                //Config.SaveData();

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
            var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, keyChips[0], async a => await EditMessage($"正在获取数据... ({a}/15)", update, selfMessage.MessageId));
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
        var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, keyChips[0], a => { });
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
        var group = userMsg.Group;

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
        if (maiAccountList is null || ratingList is null || top is null)
            CalRating();

        var cmd = (Command)userMsg.Command!;
        var querier = userMsg.From;
        var param = cmd.Params.Skip(1).ToArray();

        if (!param.IsEmpty())
        {
            if (param.First() == "refresh")
            {
                CalRating();
                userMsg.Reply("Mai rank have been updated ~");
                return;
            }
            else
            {
                //GetHelpInfo(userMsg);
                return;
            }
        }

        var rank = top!.Select(x => x.ToList()).ToList();
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
        var extension = ScriptManager.GetExtension("MaiMonitor");


        if(extension is IMonitor<Dictionary<string,string>> monitor)
        {
            var cmd = (Command)userMsg.Command!;
            var querier = userMsg.From;
            var param = cmd.Params.Skip(1).ToArray();
            if (cmd.Prefix == "maistatus")
                param = cmd.Params;

            var result = monitor.GetResult();


            string text = "";
            if (param.IsEmpty())
            {
                text = "maimai服务器状态:\n" +
                          "```python" +
                         StringHandle(
                          "\nTcping延迟:" +
                         $"\n  - Title服务器  : {result["tAvgPing"]}ms" +
                         $"\n  - OAuth服务器  : {result["oAvgPing"]}ms" +
                         $"\n  - DXNet服务器  : {result["nAvgPing"]}ms" +
                         $"\n  - Main 服务器  : {result["mAvgPing"]}ms" +
                         $"\n" +
                         $"响应包跳过率 : \n" +
                         $"  - 30min  : {result["skipRate1"]}%\n" +
                         $"  - 60min  : {result["skipRate2"]}%\n" +
                         $"  - 90min  : {result["skipRate3"]}%\n" +
                         $"\n") +
                          "```";
            }
            else if (param.First() is "full")
            {
                text = "maimai服务器状态:\n" +
                          "```python" +
                         StringHandle(
                          "\nTcping延迟:" +
                         $"\n- Title服务器  : {result["tAvgPing"]}ms\n" +
                         $"  -  5min  : {result["tAvgPing1"]}ms\n" +
                         $"  - 10min  : {result["tAvgPing2"]}ms\n" +
                         $"  - 15min  : {result["tAvgPing3"]}ms" +
                         $"\n- OAuth服务器  : {result["oAvgPing"]}ms\n" +
                         $"  -  5min  : {result["oAvgPing1"]}ms\n" +
                         $"  - 10min  : {result["oAvgPing2"]}ms\n" +
                         $"  - 15min  : {result["oAvgPing3"]}ms" +
                         $"\n- DXNet服务器  : {result["nAvgPing"]}ms\n" +
                         $"  -  5min  : {result["nAvgPing1"]}ms\n" +
                         $"  - 10min  : {result["nAvgPing2"]}ms\n" +
                         $"  - 15min  : {result["nAvgPing3"]}ms" +
                         $"\n- Main 服务器  : {result["mAvgPing"]}ms\n" +
                         $"  -  5min  : {result["mAvgPing1"]}ms\n" +
                         $"  - 10min  : {result["mAvgPing2"]}ms\n" +
                         $"  - 15min  : {result["mAvgPing3"]}ms" +
                         $"\n\n" +
                          "响应状态:\n" +
                         $"- 发送包数累计 : {result["totalRequestCount"]}\n" +
                         $"- 响应超时累计 : {result["timeoutRequestCount"]}\n" +
                         $"- 其他错误累计 : {result["otherErrorCount"]}\n" +
                         $"- 非压缩包累计 : {result["compressSkipRequestCount"]}\n" +
                         $"- 响应包跳过率 : \n" +
                         $"  - 30min  : {result["skipRate1"]}%\n" +
                         $"  - 60min  : {result["skipRate2"]}%\n" +
                         $"  - 90min  : {result["skipRate3"]}%\n" +
                         $"- 最新一次响应 : {result["statusCode"]}\n\n" +
                         $"\n") +
                          "```";
            }
            else
                text = $"\"{string.Join(" ", param)}\"为无效参数喵x";

            await userMsg.Reply(text, ParseMode.MarkdownV2);
        }
        else
            userMsg.Reply("Internal error: Module\"MaiMonitor\" not found");
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
            _ => "Unknown"
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
            case "maiscanner":
                helpStr += StringHandle(
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
    void CalRating()
    {
        if (maiAccountList is null)
            maiAccountList = new (maiDatabase.All());
        var allRating = maiAccountList.OrderBy(x => x.playerRating);
        ratingList = allRating.OrderByDescending(x => x.playerRating).Select(x => x.playerRating).ToList();
        var top = allRating.Skip(allRating.Count() - 300).OrderByDescending(x => x.playerRating);
        var ratingGroup = top.GroupBy(x => x.playerRating);

        this.top = ratingGroup.ToList();
    }
    async Task<long> GetUserRank(long rating)
    {
        if (maiAccountList is null || ratingList is null || top is null)
            CalRating();
        return await Task.Run(() =>
        {
            var rankList = ratingList!.GroupBy(x => x);
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
}
public partial class Mai
{
    
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