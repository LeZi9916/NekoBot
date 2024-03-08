using System.Collections.Generic;

namespace TelegramBot.Class
{
    class Group
    {
        public long GroupId { get; set; }
        public BotConfig Setting { get; set; } = new();
        public List<FilterRule> Rules { get; set; } = new();
        public Permission Level { get; set; } = Permission.Common;
        public void SetPermission(Permission targetLevel)
        {
            Level = targetLevel;
            Config.SaveData();
        }
    }
}
