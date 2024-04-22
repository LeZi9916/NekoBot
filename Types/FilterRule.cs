using Telegram.Bot.Types.Enums;

namespace TelegramBot.Types;
public class FilterRule
{
    public required User Target { get; set; }
    public required Action Action { get; set; }
    public required MessageType MessageType { get; set; }
    public string MatchString { get; set; }
    public string ReplyString { get; set; }
}
