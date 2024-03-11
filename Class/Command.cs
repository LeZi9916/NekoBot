
namespace TelegramBot.Class
{
    struct Command
    {
        public CommandType Prefix;
        public string[] Content;

        public static CommandType GetCommandType(string s)
        {
            switch (s)
            {
                case "start":
                    return CommandType.Start;
                case "add":
                    return CommandType.Add;
                case "ban":
                    return CommandType.Ban;
                case "bind":
                    return CommandType.Bind;
                case "status":
                    return CommandType.Status;
                case "help":
                    return CommandType.Help;
                case "info":
                    return CommandType.Info;
                case "promote":
                    return CommandType.Promote;
                case "demote":
                    return CommandType.Demote;
                case "mai":
                    return CommandType.Mai;
                case "maistatus":
                    return CommandType.MaiStatus;
                case "config":
                    return CommandType.Config;
                case "logs":
                    return CommandType.Logs;
                case "set":
                    return CommandType.Set;
                case "maiscanner":
                    return CommandType.MaiScanner;
                default:
                    return CommandType.Unknow;
            }
        }
    }
}
