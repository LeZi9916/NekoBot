using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Action = System.Action;

namespace NekoBot.Interfaces;

public interface IHandler: IExtension
{
    Action? Handle(in List<IExtension> extensions,Update update);
}
