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

namespace TelegramBot
{
    internal partial class Program
    {
        static void MaiCommandHandle(Command command, Update update, TUser querier)
        {
            if (!querier.CheckPermission(Permission.Advanced))
                return ;
            if (command.Content.Length == 0)
            {
                GetHelpInfo(command, update, querier);
                return;
            }

            var suffix = command.Content[0];
            command.Content = command.Content.Skip(1).ToArray();
            if (suffix != "bind" && querier.MaiUserId is null)
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
                case "logout":
                    Logout(command,update, querier);
                    break;
            }

        }
        static void GetMaiUserId(Command command, Update update, TUser querier)
        {

        }
        static void GetMaiUserInfo(Command command, Update update, TUser querier)
        {
            var maiUserId = querier.MaiUserId;
            var response = GetUserPreview((int)maiUserId).Result.Object;

            SendMessage(
                "用户信息:\n" +
                $"名称: {response.userName}\n" +
                $"Rating: {response.playerRating}\n" +
                $"最后游玩日期: {response.lastPlayDate}\n" +
                $"对端响应: {response.StatusCode}", update);
        }
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

                        maiUserId = QRCode.FromImage(filePath).Object.userID;
                    }
                    else
                    {
                        EditMessage("绑定失败，图片下载失败QAQ", update, selfMessage.MessageId);
                        return;
                    }
                }
                else if (param.Length > 13 && param.Substring(0, 8) == "SGWCMAID")
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
        static void Logout(Command command, Update update, TUser querier)
        {
            var request = new Request<UserLogoutRequest>(new UserLogoutRequest() { userId = (int)querier.MaiUserId });

            var result = Aqua.PostAsync<UserLogoutRequest,UserLogoutResponse>(request).Result;

            if(result is not null)
                SendMessage("已发信，请检查是否生效~\n" +
                           $"对端响应: {result.Object.StatusCode}", update);
            else
                SendMessage("发信失败QAQ\n" +
                           $"对端响应: {result.Object.StatusCode}", update);

        }

        static void GetMaiServerStatus(Command command, Update update, TUser querier)
        {
            string text = "maimai服务器状态:\n" +
                          "```csharp" +
                         StringHandle(
                          "\nTcping延迟:" +
                         $"\n- Title服务器  : {MaiMonitor.TitleServerDelay}ms\n" +
                         $"  -  5min  : {MaiMonitor.Get5minAvgPing(MaiMonitor.ServerType.Title)}ms\n" +
                         $"  - 10min  : {MaiMonitor.Get10minAvgPing(MaiMonitor.ServerType.Title)}ms\n" +
                         $"  - 15min  : {MaiMonitor.Get15minAvgPing(MaiMonitor.ServerType.Title)}ms" +
                         $"\n- OAuth服务器  : {MaiMonitor.OAuthServerDelay}ms\n" +
                         $"  -  5min  : {MaiMonitor.Get5minAvgPing(MaiMonitor.ServerType.OAuth)}ms\n" +
                         $"  - 10min  : {MaiMonitor.Get10minAvgPing(MaiMonitor.ServerType.OAuth)}ms\n" +
                         $"  - 15min  : {MaiMonitor.Get15minAvgPing(MaiMonitor.ServerType.OAuth)}ms" +
                         $"\n- DXNet服务器  : {MaiMonitor.NetServerDelay}ms\n" +
                         $"  -  5min  : {MaiMonitor.Get5minAvgPing(MaiMonitor.ServerType.Net)}ms\n" +
                         $"  - 10min  : {MaiMonitor.Get10minAvgPing(MaiMonitor.ServerType.Net)}ms\n" +
                         $"  - 15min  : {MaiMonitor.Get15minAvgPing(MaiMonitor.ServerType.Net)}ms" +
                         $"\n- Main 服务器  : {MaiMonitor.MainServerDelay}ms\n" +
                         $"  -  5min  : {MaiMonitor.Get5minAvgPing(MaiMonitor.ServerType.Main)}ms\n" +
                         $"  - 10min  : {MaiMonitor.Get10minAvgPing(MaiMonitor.ServerType.Main)}ms\n" +
                         $"  - 15min  : {MaiMonitor.Get15minAvgPing(MaiMonitor.ServerType.Main)}ms" +
                         $"\n\n" +
                          "响应状态:\n" +
                         $"- 发送包数累计 : {MaiMonitor.TotalRequestCount}\n" +
                         $"- 响应超时累计 : {MaiMonitor.TimeoutRequestCount}\n" +
                         $"- 非压缩包累计 : {MaiMonitor.CompressSkipRequestCount}\n" +
                         $"- 响应包跳过率 : {MaiMonitor.CompressSkipRate * 100}%\n\n" +
                         $"土豆性:\n" +
                         $"-       土豆？: {(MaiMonitor.ServiceAvailability ? MaiMonitor.CompressSkipRate > 0.18 ? "差不多熟了" : "否" : "是")}\n" +
                         $"- 平均土豆间隔 : {(MaiMonitor.FaultInterval == -1 ?"不可用" : $"{MaiMonitor.FaultInterval}s")}\n" +
                         $"\n") +
                          "```";

            SendMessage(text, update,true,ParseMode.MarkdownV2);
        }
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
}
