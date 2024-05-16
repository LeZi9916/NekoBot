using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Types
{
    public enum Permission
    {
        Unknown = -1,
        Ban,
        Common,
        Advanced,
        Admin,
        Root
    }
    public enum Action
    {
        Ban,
        Reply,
        Delete
    }
    public enum Range
    {
        Group,
        Global
    }
    enum CommandType
    {
        Start,
        Add,
        Ban,
        Bind,
        Status,
        Help,
        Info,
        Promote,
        Demote,
        Mai,
        Logs,
        Config,
        Set,
        MaiStatus,
        MaiScanner,
        Unknow
    }
}
