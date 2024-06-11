using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
namespace NekoBot.Types;
public class CallbackMsg
{
    public required string Id { get; set; }
    public required User From { get; set; }
    public required Message Origin { get; set; }
    public string? Data { get; set; }
    public required ITelegramBotClient Client { get; set; }
}
public delegate (bool,bool) CallbackHandler<in T>(T msg);