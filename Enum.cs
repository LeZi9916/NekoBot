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
}
