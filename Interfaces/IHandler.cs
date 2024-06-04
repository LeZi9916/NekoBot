using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Action = System.Action;

namespace NekoBot.Interfaces;

public interface IHandler: IExtension
{
    Action? Handle(in ITelegramBotClient client,in List<IExtension> extensions,Update update);
}
public interface IHandler<T> : IHandler
{
    event Action<T> OnCallback;
}