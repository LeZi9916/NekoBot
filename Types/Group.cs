﻿using NekoBot.Interfaces;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace NekoBot.Types;
public class Group : IAccount
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public BotConfig Setting { get; set; } = new();
    public List<FilterRule> Rules { get; set; } = new();
    public Permission Level { get; set; } = Permission.Common;
    public void SetPermission(Permission targetLevel)
    {
        Level = targetLevel;
    }
    public bool CheckPermission(Permission targetLevel) => Level >= targetLevel;
    public static void Update(Update update)
    {

    }
}
