using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TelegramBot.MaiScanner;
using Telegram.Bot.Types;
using System.Text.Json.Serialization;
using static TelegramBot.MaiDatabase;

namespace TelegramBot.Class
{
    public class TUser: IAccount
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name
        {
            get => FirstName + " " + LastName;
        }
        public Permission Level { get; set; } = Permission.Common;
        public int? MaiUserId { get; set; } = null;
        public bool isBanned => Level <= Permission.Ban;
        public bool isUnknow => Level == Permission.Unknow;
        public bool isNormal => !(Id is 136817688 or 1087968824);
        [JsonIgnore]
        public MaiAccount Account { get; set; }
        public async Task<bool> GetMaiAccountInfo()
        {
            return await Task.Run(() =>
            {
                if (MaiUserId is null)
                    return false;

                var userid = (int)MaiUserId;
                var result = MaiAccountList.Where(x => x.userId == userid);

                if (result.Count() == 0)
                    return false;

                Account = result.ToArray()[0];
                return true;
            });
        }
        public bool CheckPermission(Permission targetLevel) => Level >= targetLevel;
        public bool CheckPermission(Permission targetLevel, Group group)
        {
            if (group is not null && group.CheckPermission(targetLevel))
                return Level >= Permission.Common;
            return Level >= targetLevel;
        }
        public void SetPermission(Permission targetLevel)
        {
            Level = targetLevel;
            Config.SaveData();
        }
        public static void Update(Update update)
        {
            var users = GetUsers(update);

            foreach (var user in users)
            {
                if (user is null)
                    continue;

                var target = Config.SearchUser(user.Id);
                var _user = TUser.FromUser(user);

                if (target is null)
                    continue;
                else if (target.Equals(_user))
                    continue;
                else
                {
                    target.Username = _user.Username;
                    target.FirstName = _user.FirstName;
                    target.LastName = _user.LastName;

                    Program.Debug(DebugType.Info, $"User info had been updated:\n" +
                    $"Name: {user.FirstName} {user.LastName}\n" +
                    $"isBot: {user.IsBot}\n" +
                    $"Username: {user.Username}\n" +
                    $"isPremium: {user.IsPremium}");
                }
            }
        }
        public static User[] GetUsers(Update update)
        {
            var message = update.Message ?? update.EditedMessage ?? update.ChannelPost ?? update.EditedChannelPost;
            var request = update.ChatJoinRequest;

            if (message is null)
                return null;

            User[] userList = new User[5];
            userList[0] = message.From;
            userList[1] = message.ForwardFrom;
            userList[2] = message?.ReplyToMessage?.From;
            userList[3] = message?.ReplyToMessage?.ForwardFrom;
            userList[4] = request?.From;
            //if (message.ReplyToMessage is not null)
            //{
            //    userList[2] = message?.ReplyToMessage?.From;
            //    userList[3] = message.ReplyToMessage.ForwardFrom;
            //}

            return userList;
        }
        public bool Equals(TUser user) => user.Username == Username && user.FirstName == FirstName && user.LastName == LastName;
        public static TUser FromUser(User user) => new TUser()
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
