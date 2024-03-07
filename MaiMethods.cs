using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AquaTools;
using AquaTools.Requests;
using AquaTools.Responses;
using System;
using static TelegramBot.MaiDatabase;
using static TelegramBot.MaiScanner;
using AquaTools.Exception;
using System.Collections.Generic;
using AquaTools.Users;

namespace TelegramBot
{
    internal partial class Program
    {
        static void MaiCommandHandle(Command command, Update update, TUser querier, Group group = null)
        {
            if (!querier.CheckPermission(Permission.Advanced,group))
            {
                SendMessage("Permission Denied", update, true);
                return;
            }
            if (command.Content.Length == 0)
            {
                GetHelpInfo(command, update, querier);
                return;
            }

            var suffix = command.Content[0];
            command.Content = command.Content.Skip(1).ToArray();
            if (suffix is not ("bind" or "status" or "rank") && querier.MaiUserId is null)
            {
                SendMessage("你还没有绑定账号喵x", update);
                return;
            }
            switch (suffix)
            {
                case "status":
                    GetMaiServerStatus(command, update, querier);
                    break;
                case "region":
                    GetMaiUserRegion(command, update, querier);
                    break;
                case "info":
                    GetMaiUserInfo(command,update, querier);
                    break;
                case "bind":
                    BindUser(command,update, querier);
                    break;
                case "rank":
                    GetMaiTopRank(command, update, querier);
                    break;
                case "logout":
                    Logout(command,update, querier);
                    break;
                case "backup":
                    MaiDataBackup(command, update, querier);
                    break;
                case "sync":
                    UpdateMaiUserData(command, update, querier);
                    break;
                case "ticket":
                    GetMaiTicket(command, update, querier);
                    break;
                    //case "upsert":
                    //    MaiUpsert(command, update, querier);
                    //    break;
            }

        }
        static void GetMaiUserId(Command command, Update update, TUser querier)
        {

        }
        /// <summary>
        /// 获取maimai账号信息
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static async void GetMaiUserInfo(Command command, Update update, TUser querier)
        {
            //var maiUserId = querier.MaiUserId;
            //var response = GetUserPreview((int)maiUserId).Result.Object;
            MaiAccount account = querier.Account;
            Func<int, Task<MaiAccount?>> getAccount = async userid => 
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

                    MaiAccountList.Add(maiAccount);
                    Config.SaveData();
                    return maiAccount;
                }
                else
                {
                    SendMessage("获取数据失败QAQ", update);
                    return null;
                }
            };

            if(command.Content.Length == 1)
            {
                int id;
                if (!querier.CheckPermission(Permission.Admin))
                {
                    SendMessage("Access denied", update);
                    return;
                }
                else if (!int.TryParse(command.Content[0], out id))
                {
                    SendMessage("请确认参数是Int32~", update);
                    return;
                }

                account = MaiDatabase.Search(id);

                if (account is null)
                    account = await getAccount(id);
            }
            else if(command.Content.Length > 1)
            {
                SendMessage("参数错误QAQ", update);
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

            var message = await SendMessage(
                "用户信息:\n" +
                $"名称: {account.userName}\n" +
                $"Rating: {account.playerRating}\n" +
                $"排名: 计算中...\n" +
                $"Rom版本: {account.lastRomVersion}\n" +
                $"Data版本: {account.lastDataVersion}\n" +
                $"DX主要版本: {account.lastGameId}\n" +
                $"最后同步日期: {account.lastUpdate.ToString("yyyy-MM-dd HH:mm:ss")}", update);

            var ranking = await GetUserRank(account.playerRating);

            EditMessage(
                "用户信息:\n" +
                $"名称: {account.userName}\n" +
                $"Rating: {account.playerRating}\n" +
                $"排名: {ranking}\n" +
                $"Rom版本: {account.lastRomVersion}\n" +
                $"Data版本: {account.lastDataVersion}\n" +
                $"DX主要版本: {account.lastGameId}\n" +
                $"最后同步日期: {account.lastUpdate.ToString("yyyy-MM-dd HH:mm:ss")}", update, message.MessageId);


        }
        /// <summary>
        /// 获取登录地
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static void GetMaiUserRegion(Command command, Update update, TUser querier)
        {
            var request = new Request<UserRegionRequest>();
            request.Object.userId = (int)querier.MaiUserId;

            var response = Aqua.Post<UserRegionRequest,UserRegionResponse>(request).Object;
            string regionStr = "";
            int totalPlayCount = 0;
            DateTime firstRegionDate = DateTime.Now;

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                SendMessage("获取出勤地区数据失败QAQ\n" +
                           $"对端响应: {response.StatusCode}", update);
                return;
            }
            if(response.userRegionList.Length == 0)
            {
                SendMessage("你看起来从未出过勤呢~", update);
                return;
            }
            foreach(var region in response.userRegionList)
            {
                regionStr += $"\n\\- *{GetRegionName(region.RegionId)} *\n" +
                StringHandle($"   最早出勤于:{region.CreateDate.ToString("yyyy/MM/dd")}\n" +
                             $"   出勤次数: {region.PlayCount}\n");

                totalPlayCount += region.PlayCount;
                if(region.CreateDate.Ticks < firstRegionDate.Ticks)
                    firstRegionDate = region.CreateDate;
            }
            SendMessage("你的出勤数据如下:\n" + regionStr +
                        $"\n你最早在{firstRegionDate.ToString("yyyy/MM/dd")}出勤；在过去的{(DateTime.Now - firstRegionDate).Days}天里，你一共出勤了{totalPlayCount}次", update,true,ParseMode.MarkdownV2);
        }
        /// <summary>
        /// 绑定maimai账号
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static void BindUser(Command command, Update update, TUser querier)
        {
            try
            {
                var message = update.Message;
                var chat = update.Message.Chat;
                var param = command.Content[0];
                int? maiUserId = null;
                var filePath = Path.Combine(Config.TempPath, $"{GetRandomStr()}".Replace("\\", "").Replace("/", ""));
                var isPrivate = chat.Type is ChatType.Private;
                if (!isPrivate)
                {
                    SendMessage("喵呜呜", update, false);
                    return;
                }
                var selfMessage = SendMessage("已收到请求，请耐心等待处理~", update, false).Result;

                Thread.Sleep(500);

                if (querier.MaiUserId is not null)
                {
                    EditMessage("不能重复绑定账号喵x", update, selfMessage.MessageId);
                    return;
                }

                if (param.ToLower() == "image")
                {
                    if (message.Photo is null)
                    {
                        EditMessage("图片喵?", update, selfMessage.MessageId);
                        return;
                    }
                    var photoSize = message.Photo.Last();

                    selfMessage = EditMessage("正在下载图片...", update, selfMessage.MessageId).Result;

                    if (DownloadFile(filePath, photoSize.FileId).Result)
                    {
                        selfMessage = EditMessage("图片下载完成", update, selfMessage.MessageId).Result;
                        Thread.Sleep(500);
                        selfMessage = EditMessage("正在解析二维码...", update, selfMessage.MessageId).Result;
                        Thread.Sleep(500);

                        maiUserId = QRCode.ToUserId(Image.FromFile(filePath)).Object.userID;
                    }
                    else
                    {
                        EditMessage("绑定失败，图片下载失败QAQ", update, selfMessage.MessageId);
                        return;
                    }
                }
                else if (QRCode.IsWeChatId(param))
                    maiUserId = QRCode.ToUserId(param).Object.userID;
                else
                {
                    SendMessage("这是什么喵?", update, true);
                    return;
                }

                if (maiUserId == -1)
                {
                    EditMessage("你的二维码看上去已经过期了呢，请重新获取喵x", update, selfMessage.MessageId);
                    return;
                }

                selfMessage = EditMessage("正在获取用户信息...", update, selfMessage.MessageId).Result;
                var response = GetUserPreview((int)maiUserId).Result.Object;
                querier.MaiUserId = maiUserId;
                querier.GetMaiAccountInfo();
                if (response.StatusCode is not HttpStatusCode.OK)
                {
                    EditMessage("绑定成功，但无法获取用户信息QAQ", update, selfMessage.MessageId);
                    return;
                }

                selfMessage = EditMessage(
                    "绑定成功\\!\n\n" +
                    "用户信息:\n" + StringHandle(
                    $"名称: {response.userName}\n" +
                    $"Rating: {response.playerRating}\n" +
                    $"最后游玩日期: {response.lastPlayDate}"), update, selfMessage.MessageId, parseMode: ParseMode.MarkdownV2).Result;

                Config.SaveData();
                System.IO.File.Delete(filePath);
            }
            catch
            {
                SendMessage("参数错误喵x", update);
            }
        }
        static async void MaiUserLogin(Command command, Update update, TUser querier)
        {
            var user = await AquaTools.Users.User.Login((int)querier.MaiUserId, Config.keyChips[0],a => { });
            return;
        }
        /// <summary>
        /// 备份用户数据
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static async void MaiDataBackup(Command command, Update update, TUser querier)
        {
            int userid = (int)querier.MaiUserId;
            string password = "";

            if (command.Content.Length < 1)
            {
                SendMessage("缺少参数喵x", update);
                return;
            }
            if (command.Content.Length == 2)
            {
                if (!int.TryParse(command.Content[0],out userid))
                {
                    SendMessage("缺少参数喵x", update);
                    return;
                }
                password = command.Content[1];
            }
            else
                password = command.Content[0];


            var selfMessage = await SendMessage("已收到请求，请耐心等待处理~", update);

            EditMessage("正在尝试登录... (0/15)", update, selfMessage.MessageId);
            try
            {
                var user = await AquaTools.Users.User.Login(userid, Config.keyChips[0], async a => await EditMessage($"正在获取数据... ({a}/15)", update, selfMessage.MessageId));
                await EditMessage("获取数据成功,正在上传备份文件...", update, selfMessage.MessageId);
                var userdata = user.Export(password);                
                var stream = new MemoryStream(userdata);
                await UploadFile(stream, $"UserDataBackup{DateTime.Now.ToString("yyyyMMddhhmm")}.data", update.Message.Chat.Id);
                EditMessage("数据备份完成喵x", update, selfMessage.MessageId);
                user.Logout();
            }
            catch(LoginFailureException e)
            {
                
                EditMessage("登录失败,请检查二维码是否过期QAQ\n" +
                    $"```csharp\n" +
                    $"{StringHandle($"{e.Message}")}\n" +
                    $"```", update, selfMessage.MessageId, ParseMode.MarkdownV2);
                
            }
            catch(Exception e)
            {
                EditMessage($"出现未知错误QAQ\n" +
                    $"```csharp\n" +
                    $"{StringHandle($"{e.Message}")}\n" +
                    $"```", update, selfMessage.MessageId,ParseMode.MarkdownV2);
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
        static async void UpdateMaiUserData(Command command, Update update, TUser querier)
        {
            var selfMessage = await SendMessage("已收到请求，请耐心等待处理~", update);
            int userId;
            if (command.Content.Length == 1)
            {
                if(!querier.CheckPermission(Permission.Admin))
                {
                    EditMessage("Access denied", update, selfMessage.MessageId);
                    return;
                }
                else if (!int.TryParse(command.Content[0],out userId))
                {
                    EditMessage("请确认参数是Int32~", update, selfMessage.MessageId);
                    return;
                }
            }
            else if(command.Content.Length > 1)
            {
                EditMessage("参数错误QAQ", update, selfMessage.MessageId);
                return;
            }
            else
                userId = (int)querier.MaiUserId;

            try
            {
                var maiUser = MaiDatabase.Search(userId);
                bool isNew = maiUser == null;
                var response = (await GetUserPreview(userId)).Object;

                if (maiUser is null)
                    maiUser = new MaiAccount();
                maiUser.playerRating = response.playerRating ?? 0;
                maiUser.lastDataVersion = response.lastDataVersion;
                maiUser.lastRomVersion = response.lastRomVersion;
                maiUser.lastGameId = response.lastGameId;
                maiUser.banState = response.banState;
                maiUser.lastUpdate = DateTime.Now;

                querier.Account = maiUser;
                if(isNew)
                    MaiDatabase.MaiAccountList.Add(maiUser);
                Config.SaveData();

                EditMessage("更新完成喵wAw", update, selfMessage.MessageId);
            }
            catch(Exception e)
            {
                EditMessage("发生了未知错误QAQ\n" +
                    "```csharp\n" +
                    $"{e.Message}\n" +
                    $"```", update, selfMessage.MessageId,ParseMode.MarkdownV2);
            }
        }
        static async void GetMaiTicket(Command command, Update update, TUser querier)
        {
            int count = 1;
            int ticketType = 0;
            if (command.Content.Length == 0)
            {
                GetHelpInfo(command, update, querier);
                return;
            }

            var selfMessage = await SendMessage("已收到请求，请耐心等待处理~", update);
            Dictionary<string, int> vaildTicketType = new()
            { 
                { "2",2 } ,
                { "3",3 } ,
                { "5",5 } ,
                { "20",20020 } ,
            };

            
            if (command.Content.Length < 3)
            {
                if (!vaildTicketType.ContainsKey(command.Content[0]))
                {
                    EditMessage("参数错误喵x", update, selfMessage.MessageId);
                    return;
                }
                else if (command.Content.Length == 2 && !int.TryParse(command.Content[1], out count))
                {
                    EditMessage("参数错误喵x", update, selfMessage.MessageId);
                    return;
                }
                ticketType = vaildTicketType[command.Content[0]];
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
                if(result)
                    EditMessage("跑图券获取成功wAw", update, selfMessage.MessageId);
                else
                    EditMessage("跑图券获取失败，请检查你是否已有相同的券QAQ", update, selfMessage.MessageId);
            }
            catch(Exception e)
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
        }
        static async void MaiUpsert(Command command, Update update, TUser querier)
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
                noteInfo , null, (long)user.LoginId, 1));
            playlogs.Add(user.CreateUserPlaylog(musicDetail, 
                new Dictionary<string, int>() 
                {
                    { "isRandom" , 0},
                    { "MaxCombo" , 605}
                }, 
                noteInfo , null, (long)user.LoginId, 2));
            playlogs.Add(user.CreateUserPlaylog(musicDetail, 
                new Dictionary<string, int>() 
                {
                    { "isRandom" , 0},
                    { "MaxCombo" , 605}
                }, 
                noteInfo , null, (long)user.LoginId, 3));

            var result = user.UpsertAll(playlogs.ToArray(), (long)user.LoginId);
            user.Logout();
            return;
        }
        /// <summary>
        /// 逃离小黑屋
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static void Logout(Command command, Update update, TUser querier, Group group = null)
        {
            if(command.Content.Length != 0)
            {
                if(!querier.CheckPermission(Permission.Admin,group))
                {
                    GetHelpInfo(command, update, querier);
                return;
                }
            }

            var request = new Request<UserLogoutRequest>(new UserLogoutRequest() { userId = (int)querier.MaiUserId });

            var result = Aqua.PostAsync<UserLogoutRequest,UserLogoutResponse>(request).Result;

            if(result is not null)
                SendMessage("已发信，请检查是否生效~\n" +
                           $"对端响应: {result.Object.StatusCode}", update);
            else
                SendMessage("发信失败QAQ\n" +
                           $"对端响应: {result.Object.StatusCode}", update);

        }
        /// <summary>
        /// 获取国服排行榜
        /// </summary>
        /// <param name="command"></param>
        /// <param name="update"></param>
        /// <param name="querier"></param>
        static void GetMaiTopRank(Command command, Update update, TUser querier)
        {
            if(command.Content.Length != 0)
            {
                if (command.Content[0] == "refresh")
                {
                    MaiDatabase.CalRating();
                    SendMessage("排行榜已刷新~", update);
                    return;
                }
                else
                {
                    GetHelpInfo(command, update, querier);
                    return;
                }
            }

            var rank = Top.Select(x => x.ToList()).ToList();
            var strHeader = "全国前300排行榜\n" +
                            "```markdown\n" +
                            $"{"名次".PadRight(14)}{"Rating".PadRight(16)}{"名称".PadRight(12)}\n";
            var strFooter = "```";
            int ranking = 1;
            int count = 0;
            int index = 1;
            var playerInfoStr = "";
            foreach(var playerGroup in rank)
            {
                foreach (var player in playerGroup)
                {                    
                    playerInfoStr += StringHandle($"{ranking.ToString().PadRight(14)}{player.playerRating.ToString().PadRight(16)}{player.userName.PadRight(12)}\n");
                    count++;
                    if (count == 50)
                    {
                        SendMessage(strHeader + playerInfoStr + strFooter + $"\n\\({index}\\/6\\)", update, true, ParseMode.MarkdownV2);
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
        static void GetMaiServerStatus(Command command, Update update, TUser querier)
        {
            var titlePingInfo = MaiMonitor.GetAvgPing(MaiMonitor.ServerType.Title);
            var oauthPingInfo = MaiMonitor.GetAvgPing(MaiMonitor.ServerType.OAuth);
            var netPingInfo = MaiMonitor.GetAvgPing(MaiMonitor.ServerType.Net);
            var mainPingInfo = MaiMonitor.GetAvgPing(MaiMonitor.ServerType.Main);
            var skipRateInfo = MaiMonitor.GetAvgSkipRate();
            string text = "";
            if (command.Content.Length == 0)
            {
                text = "maimai服务器状态:\n" +
                          "```python" +
                         StringHandle(
                          "\nTcping延迟:" +
                         $"\n  - Title服务器  : {MaiMonitor.TitleServerDelay}ms" +
                         $"\n  - OAuth服务器  : {MaiMonitor.OAuthServerDelay}ms" +
                         $"\n  - DXNet服务器  : {MaiMonitor.NetServerDelay}ms" +
                         $"\n  - Main 服务器  : {MaiMonitor.MainServerDelay}ms" +
                         $"\n" +
                         $"响应包跳过率 : \n" +
                         $"  -  5min  : {Math.Round(skipRateInfo[0] * 100, 2)}%\n" +
                         $"  - 10min  : {Math.Round(skipRateInfo[1] * 100, 2)}%\n" +
                         $"  - 15min  : {Math.Round(skipRateInfo[2] * 100, 2)}%\n" +
                         $"  -  Avg   : {Math.Round(MaiMonitor.CompressSkipRate * 100, 2)}%" +
                         $"\n") +
                          "```";
            }
            else if (command.Content.Length == 1 && command.Content[0] is "full")
            {
                text = "maimai服务器状态:\n" +
                          "```python" +
                         StringHandle(
                          "\nTcping延迟:" +
                         $"\n- Title服务器  : {MaiMonitor.TitleServerDelay}ms\n" +
                         $"  -  5min  : {titlePingInfo[0]}ms\n" +
                         $"  - 10min  : {titlePingInfo[1]}ms\n" +
                         $"  - 15min  : {titlePingInfo[2]}ms" +
                         $"\n- OAuth服务器  : {MaiMonitor.OAuthServerDelay}ms\n" +
                         $"  -  5min  : {oauthPingInfo[0]}ms\n" +
                         $"  - 10min  : {oauthPingInfo[1]}ms\n" +
                         $"  - 15min  : {oauthPingInfo[2]}ms" +
                         $"\n- DXNet服务器  : {MaiMonitor.NetServerDelay}ms\n" +
                         $"  -  5min  : {netPingInfo[0]}ms\n" +
                         $"  - 10min  : {netPingInfo[1]}ms\n" +
                         $"  - 15min  : {netPingInfo[2]}ms" +
                         $"\n- Main 服务器  : {MaiMonitor.MainServerDelay}ms\n" +
                         $"  -  5min  : {mainPingInfo[0]}ms\n" +
                         $"  - 10min  : {mainPingInfo[1]}ms\n" +
                         $"  - 15min  : {mainPingInfo[2]}ms" +
                         $"\n\n" +
                          "响应状态:\n" +
                         $"- 发送包数累计 : {MaiMonitor.TotalRequestCount}\n" +
                         $"- 响应超时累计 : {MaiMonitor.TimeoutRequestCount}\n" +
                         $"- 其他错误累计 : {MaiMonitor.OtherErrorCount}\n" +
                         $"- 非压缩包累计 : {MaiMonitor.CompressSkipRequestCount}\n" +
                         $"- 响应包跳过率 : \n" +
                         $"  -  5min  : {Math.Round(skipRateInfo[0] * 100, 2)}%\n" +
                         $"  - 10min  : {Math.Round(skipRateInfo[1] * 100, 2)}%\n" +
                         $"  - 15min  : {Math.Round(skipRateInfo[2] * 100, 2)}%\n" +
                         $"  -  Avg   : {Math.Round(MaiMonitor.CompressSkipRate * 100, 2)}%\n" +
                         $"- 最新一次响应 : {MaiMonitor.LastResponseStatusCode}\n\n" +
                         $"土豆性:\n" +
                         $"-       土豆？: {(MaiMonitor.ServiceAvailability ? MaiMonitor.CompressSkipRate > 0.18 ? "差不多熟了" : "新鲜的" : "熟透了")}\n" +
                         $"- 平均土豆间隔 : {(MaiMonitor.FaultInterval == -1 ? "不可用" : $"{MaiMonitor.FaultInterval}s")}\n" +
                         $"\n") +
                          "```";
            }
            else
                text = $"\"{string.Join(" ",command.Content)}\"为无效参数喵x";

            SendMessage(text, update,true,ParseMode.MarkdownV2);
        }
        /// <summary>
        /// 获取RegionId对应的地区名
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        static string GetRegionName(int regionId)
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
        static async Task<Response<UserPreviewResponse>> GetUserPreview(int userId)
        {
            var request = new Request<UserPreviewRequest>();
            request.Object.userId = userId;

            return await Aqua.PostAsync<UserPreviewRequest,UserPreviewResponse>(request);
        }
        
    }
    internal partial class Program
    {
        static void MaiScannerHandle(Command command, Update update, TUser querier,Group group = null)
        {
            if (!querier.CheckPermission(Permission.Admin, group))
            {
                SendMessage("喵?", update);
                return;
            }
            if (command.Content.Length == 0)
            {
                GetHelpInfo(command, update, querier);
                return;
            }

            var suffix = command.Content[0];
            command.Content = command.Content.Skip(1).ToArray();
            switch (suffix)
            {
                case "status":
                    GetScannerStatus(command, update, querier);
                    break;
                case "update":
                    SetScannerUpdate(command, update, querier);
                    break;
                case "stop":
                    SetScannerStop(command, update, querier);
                    break;
                case "set":
                    SetScannerConfig(command, update, querier);
                    break;
            }
        }
        static void GetScannerStatus(Command command, Update update, TUser querier)
        {
            SendMessage(
                 "目前状态:\n" +
                $"运行状态 : {MaiScanner.isRunning}\n" +
                $"总数量 : {MaiScanner.TotalAccountCount}\n" +
                $"已完成 : {MaiScanner.CurrentAccountCount}\n" +
                $"\n" +
                $"Current Qps : {MaiScanner.CurrentQps}\n" +
                $"Qps Limit   : {MaiScanner.QpsLimit}", update);
        }
        static void SetScannerStop(Command command, Update update, TUser querier)
        {
            MaiScanner.isRunning = false;
            MaiScanner.cancelSource.Cancel();
            SendMessage("已执行~", update);
        }
        static void SetScannerConfig(Command command, Update update, TUser querier)
        {
            if (command.Content.Length != 0)
            {
                int index = 0;
                if (int.TryParse(command.Content[0], out index))
                {
                    MaiScanner.QpsLimit = index;
                    SendMessage("已执行~", update);
                    return;
                }
            }
            SendMessage("参数错误喵x", update);
            return;
        }
        static void SetScannerUpdate(Command command, Update update, TUser querier)
        {
            if(command.Content.Length != 0)
            {
                int index = 0;
                if (int.TryParse(command.Content[0],out index))
                    MaiScanner.Update(index);
                else
                {
                    SendMessage("参数错误喵x", update);
                    return;
                }
            }
            else
                MaiScanner.Update();
            SendMessage("已执行~", update);
        }
    }
}
