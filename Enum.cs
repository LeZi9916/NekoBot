using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    enum Permission
    {
        Unknow = -1,
        Ban,
        Common,
        Advanced,
        Admin,
        Root
    }
    enum Action
    {
        Ban,
        Reply,
        Delete
    }
    enum Range
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
