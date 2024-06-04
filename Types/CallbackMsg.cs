using System;
using System.Collections.Generic;
using System.Linq;
namespace NekoBot.Types;
public class CallbackMsg
{
    public required User From { get; set; }
    public required Message Origin { get; set; }
    public string? Data { get; set; }
}