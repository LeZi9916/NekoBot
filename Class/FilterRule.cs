using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Class
{
    public class FilterRule
    {
        public required TUser Target { get; set; }
        public required Action Action { get; set; }
        public required MessageType MessageType { get; set; }
        public string MatchString { get; set; }
        public string ReplyString { get; set; }
    }
}
