using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AquaTools;

namespace TelegramBot
{
    internal partial class Program
    {
        static void MaiCommandHandle(Command command, Update update, TUser querier)
        {
            if (!querier.CheckPermission(Permission.Advanced))
                return ;
            if (command.Content.Length == 0)
                return;

            var suffix = command.Content[0];
            command.Content = command.Content.Skip(1).ToArray();
            if (suffix != "bind" && string.IsNullOrEmpty(querier.MaiUserId))
            {
                SendMessage("你还没有绑定账号喵x", update);
                return;
            }

            switch (suffix)
            {
                case "2userId":
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
            var userInfo = GetUserPreview(maiUserId).Result;

            SendMessage(StringHandle(
                "用户信息:\n" +
                $"名称: {userInfo["userName"]}\n" +
                $"Rating: {userInfo["playerRating"]}\n" +
                $"最后游玩日期: {userInfo["lastPlayDate"]}"), update);
        }
        static void BindUser(Command command, Update update, TUser querier)
        {
            var message = update.Message;
            var chat = update.Message.Chat;
            var param = command.Content[0];
            string maiUserId = null;
            var filePath = Path.Combine(Config.TempPath, $"{GetRandomStr()}".Replace("\\", "").Replace("/", ""));
            var isPrivate = chat.Type is ChatType.Private;
            if(!isPrivate)
            {
                SendMessage("喵呜呜", update, false);
                return;
            }
            var selfMessage = SendMessage("已收到请求，请耐心等待处理~", update, false).Result;            

            Thread.Sleep(500);

            if (!string.IsNullOrEmpty(querier.MaiUserId))
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

                    maiUserId = QRCode.FromImage(filePath).Object.UserId.ToString();
                }
                else
                {
                    EditMessage("绑定失败，图片下载失败QAQ", update, selfMessage.MessageId);
                    return;
                }
            }
            else if (param.Length > 13 && param.Substring(0, 8) == "SGWCMAID")
                maiUserId = QRCode.ToUserId(param).Object.UserId.ToString();
            else
            {
                SendMessage("这是什么喵?", update, true);
                return;
            }

            if (maiUserId == "-1")
            {
                EditMessage("你的二维码看上去已经过期了呢，请重新获取喵x", update, selfMessage.MessageId);
                return;
            }

            selfMessage = EditMessage("正在获取用户信息...", update, selfMessage.MessageId).Result;
            var userInfo = GetUserPreview(maiUserId).Result;
            querier.MaiUserId = maiUserId;

            if ((HttpStatusCode)userInfo["StatusCode"] is not HttpStatusCode.OK)
            {
                EditMessage("绑定成功，但无法获取用户信息QAQ", update, selfMessage.MessageId);
                return;
            }

            selfMessage = EditMessage(
                "绑定成功\\!\n\n" +
                "用户信息:\n" +StringHandle(
                $"名称: {userInfo["userName"]}\n" +
                $"Rating: {userInfo["playerRating"]}\n" +
                $"最后游玩日期: {userInfo["lastPlayDate"]}"), update, selfMessage.MessageId,parseMode: ParseMode.MarkdownV2).Result;

            Config.SaveData();
            System.IO.File.Delete(filePath);
        }
        static void Logout(Command command, Update update, TUser querier)
        {
            var maiUserId = querier.MaiUserId;

            Aqua.PostAsync(Aqua.Api.UserLogout,new Dictionary<string, string>()
            {
                {"UserId" , maiUserId}
            });

            SendMessage("已发信，请检查是否生效~",update);
            
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
        static async Task<Dictionary<string, object>> GetUserPreview(string userId)
        {
            return await Aqua.PostAsync(Aqua.Api.GetUserPreview, new Dictionary<string, string>
            {
                {"UserId", userId}
            });
        }
    }
}
