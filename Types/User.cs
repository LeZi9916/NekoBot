using Telegram.Bot.Types;
using System.Text.Json.Serialization;
using TelegramBot.Interfaces;

namespace TelegramBot.Types
{
    public class User : IAccount
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
        public bool IsBanned => Level <= Permission.Ban;
        public bool IsUnknown => Level == Permission.Unknown;
        public bool IsNormal => !(Id is 136817688 or 1087968824);
        public bool IsBot { get; private set; } = false;
        public bool? IsPremium { get; set; }
        [JsonIgnore]
        public MaiAccount Account { get; set; }
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
        /// <summary>
        /// Use new infomation to update this instance
        /// </summary>
        /// <param name="newUser"></param>
        /// <returns>if updated,return true</returns>
        public bool Update(User newUser)
        {
            if (Equals(newUser))
                return false;

            Username = newUser.Username;
            FirstName = newUser.FirstName;
            LastName = newUser.LastName;
            IsPremium = newUser.IsPremium;

            return true;
        }
        /// <summary>
        /// Update all user in <paramref name="update"/> instance
        /// </summary>
        /// <param name="update">The update instance</param>
        public static void Update(Update update)
        {
            var users = GetUsers(update);

            foreach (var user in users)
            {
                if (user is null)
                    continue;

                var target = Config.SearchUser(user.Id);

                if (target is null)
                    continue;
                else if (!target.Update(user))
                    continue;
                else
                {
                    Program.Debug(DebugType.Info, $"User info had been updated:\n" +
                    $"Name: {user.FirstName} {user.LastName}\n" +
                    $"isBot: {user.IsBot}\n" +
                    $"Username: {user.Username}\n" +
                    $"isPremium: {user.IsPremium}");
                }
            }
        }
        /// <summary>
        /// Get all user in <paramref name="update"/> instance
        /// </summary>
        /// <param name="update"></param>
        /// <returns>Users in <paramref name="update"/></returns>
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
        /// <summary>
        /// Compare this instance to <paramref name="user"/>
        /// </summary>
        /// <param name="user"></param>
        /// <returns>if equals,return true</returns>
        public bool Equals(User user) => user.Username == Username && 
                                         user.FirstName == FirstName && 
                                         user.LastName == LastName && 
                                         user.IsPremium == IsPremium &&
                                         user.Id == Id;

        public static implicit operator User(Telegram.Bot.Types.User u)
        {
            if (u is null) return null;
            return new User()
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsBot = u.IsBot,
                IsPremium = u.IsPremium
            };
        }
    }
}
