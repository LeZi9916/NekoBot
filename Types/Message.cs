using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static NekoBot.Core;

namespace NekoBot.Types;
public class Message
{
    public required int Id { get; init; }
    public required User From { get; set; }
    public required Chat Chat { get; init; }
    public required MessageType Type { get; init; }
    public Message? ReplyTo { get; init; } = null;
    public string? Content { get; set; }
    public bool IsPrivate { get => Chat.Type == ChatType.Private; }
    public bool IsGroup { get => Chat.Type is ChatType.Group or ChatType.Supergroup; }
    public Audio? Audio { get; set; }
    public Document? Document { get; set; }
    public PhotoSize[]? Photo { get; set; }
    public Command? Command { get; set; }
    public required ITelegramBotClient Client { get; init; }
    public Group? Group { get; set; }
    public Update? Raw { get; set; }
    public InlineKeyboardMarkup? InlineMarkup { get; set; }

    public async Task<bool> GetDocument(string dPath)
    {
        try
        {
            if (Document is null)
                return false;

            return await DownloadFile(dPath, Document.FileId);
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> GetAudio(string dPath)
    {
        try
        {
            if (Audio is null)
                return false;

            return await DownloadFile(dPath, Audio.FileId);
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> GetPhoto(string dPath)
    {
        try
        {
            if (Photo is null)
                return false;

            return await DownloadFile(dPath, Photo.Last().FileId);
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> DownloadFile(string dPath, string fileId)
    {
        try
        {
            await using Stream fileStream = System.IO.File.Create(dPath);
            var file = await Client.GetInfoAndDownloadFileAsync(
                fileId: fileId,
                destination: fileStream);
            return true;
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to download file : \n{e.Message}\n{e.StackTrace}");
            return false;
        }
    }
    public async Task<Message?> Send(string text,
                                     ParseMode? parseMode = null,
                                     bool showDelButton = false,
                                     bool disableWebPreview = false,
                                     InlineKeyboardMarkup? inlineMarkup = null)
    {
        try
        {
            if (showDelButton)
            {
                if (inlineMarkup is not null)
                {
                    foreach (var row in inlineMarkup.InlineKeyboard)
                    {
                        var buttons = inlineMarkup.InlineKeyboard.SelectMany(x => x);
                        if (buttons.Any(x => x.CallbackData == "delMsg"))
                            break;
                        if (row.Count() >= 8)
                            continue;
                        else
                        {
                            row.Append(InlineKeyboardButton.WithCallbackData("Delete", "delMsg"));
                            break;
                        }
                    }
                }
                else
                    inlineMarkup = CreateButton(InlineKeyboardButton.WithCallbackData("Delete", "delMsg"));
            }
            return Parse(Client!, await Client!.SendTextMessageAsync(
                         chatId: Chat.Id,
                         text: text,
                         replyToMessageId: null,
                         parseMode: parseMode,
                         replyMarkup: inlineMarkup,
                         disableWebPagePreview: disableWebPreview))!;
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to send message : \n{e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    public async Task<bool> Delete()
    {
        try
        {
            await Client.DeleteMessageAsync(
                chatId: Chat.Id,
                messageId: Id
                );
            return true;
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Cannot delete message : \n{e.Message}\n{e.StackTrace}");
            return false;
        }
    }
    public async Task<Message?> Edit(string? text, 
                                     ParseMode? parseMode = null,
                                     bool disableWebPreview = false,
                                     InlineKeyboardMarkup? inlineMarkup = null)
    {
        try
        {
            return Parse(Client!, await Client!.EditMessageTextAsync(
                         chatId: Chat.Id,
                         messageId: Id,
                         text: text ?? Content!, 
                         parseMode: parseMode,
                         replyMarkup: inlineMarkup ?? InlineMarkup,
                         disableWebPagePreview: disableWebPreview));
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to edit message : \n{e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    public async Task<Message?> Reply(string text, 
                                      ParseMode? parseMode = null,
                                      bool showDelButton = false,
                                      bool disableWebPreview = false,
                                      InlineKeyboardMarkup? inlineMarkup = null)
    {
        try
        {
            if(showDelButton)
            {
                if (inlineMarkup is not null)
                {
                    foreach (var row in inlineMarkup.InlineKeyboard)
                    {
                        var buttons = inlineMarkup.InlineKeyboard.SelectMany(x => x);
                        if (buttons.Any(x => x.CallbackData == "delMsg"))
                            break;
                        if (row.Count() >= 8)
                            continue;
                        else
                        {
                            row.Append(InlineKeyboardButton.WithCallbackData("Delete", "delMsg"));
                            break;
                        }
                    }
                }
                else
                    inlineMarkup = CreateButton(InlineKeyboardButton.WithCallbackData("Delete", "delMsg"));
            }
            return Parse(Client!, await Client!.SendTextMessageAsync(
                         chatId: Chat.Id,
                         text: text,
                         replyToMessageId: Id,
                         parseMode: parseMode,
                         replyMarkup: inlineMarkup,
                         disableWebPagePreview: disableWebPreview))!;
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to send message : \n{e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    public async Task<Message?> AddButton(InlineKeyboardButton button)
    {
        var markup = InlineMarkup;
        if (markup is null)
            markup = CreateButtons([button]);
        else
        {
            var buttons = markup.InlineKeyboard.SelectMany(x => x);
            var newButtons = buttons.Where(x => x.Text != button.Text).ToList();
            newButtons.Add(button);
            markup = CreateButtons(newButtons.ToArray());
        }
        return await Edit(null, inlineMarkup: markup);
    }
    public async Task<Message?> DelButton(Func<InlineKeyboardButton, bool> match)
    {
        
        if (InlineMarkup is null)
            return this;
        var oldButtons = InlineMarkup.InlineKeyboard.SelectMany(x => x);
        var newButtons = oldButtons.Where(x => !match(x));
        return await Edit(null, inlineMarkup: CreateButtons(newButtons.ToArray()));
    }
    public static Message? Parse(in ITelegramBotClient client, Telegram.Bot.Types.Message? msg)
    {
        if (msg is null || msg.From is null) return null;

        var id = msg.MessageId;
        var from = msg.From;
        var content = (msg.Text ?? msg.Caption) ?? string.Empty;
        var cmd = Types.Command.Parse(content);

        return new Message()
        {
            Id = id,
            From = from!,
            Chat = msg.Chat,
            Type = msg.Type,
            ReplyTo = Parse(client, msg.ReplyToMessage),
            Audio = msg.Audio,
            Document = msg.Document,
            Photo = msg.Photo,
            Command = cmd,
            Client = client,
            Content = content,
            InlineMarkup = msg.ReplyMarkup,
        };

    }
    public static InlineKeyboardMarkup CreateButtons(InlineKeyboardButton[][] buttons) => new InlineKeyboardMarkup(buttons);
    public static InlineKeyboardMarkup CreateButtons(InlineKeyboardButton[] buttons) => CreateButtons([buttons]);
    public static InlineKeyboardMarkup CreateButton(InlineKeyboardButton button) => CreateButtons([button]);
}
