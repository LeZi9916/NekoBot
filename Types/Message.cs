using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using static TelegramBot.Core;

#nullable enable
namespace NekoBot.Types;
public class Message
{
    public required int Id { get; init; }
    public required User From { get; init; }
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
    public Group? GetGroup() => IsGroup ? Config.SearchGroup(Chat.Id) : null;
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
    public async Task<Message?> Send(string text, ParseMode? parseMode = null)
    {
        try
        {
            return Parse(Client!, await Client!.SendTextMessageAsync(
                        chatId: Chat.Id,
                        text: text,
                        replyToMessageId: null,
                        parseMode: parseMode))!;
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
            await botClient.DeleteMessageAsync(
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
    public async Task<Message?> Edit(string text, ParseMode? parseMode = null)
    {
        try
        {
            return Parse(Client!, await Client!.EditMessageTextAsync(
                    chatId: Chat.Id,
                    messageId: Id,
                    text: text, parseMode: parseMode));
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to edit message : \n{e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    public async Task<Message?> Reply(string text, ParseMode? parseMode = null)
    {
        try
        {
            return Parse(Client!, await Client!.SendTextMessageAsync(
                        chatId: Chat.Id,
                        text: text,
                        replyToMessageId: Id,
                        parseMode: parseMode))!;
        }
        catch (Exception e)
        {
            Debug(DebugType.Error, $"Failure to send message : \n{e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    public static Message? Parse(ITelegramBotClient client, Telegram.Bot.Types.Message? msg)
    {
        if (msg is null || msg.From is null) return null;

        var id = msg.MessageId;
        var from = Config.SearchUser(msg.From!.Id);
        if (from is null)
            from = msg.From;

        var content = (msg.Text ?? msg.Caption) ?? string.Empty;
        var cmd = Types.Command.Parse(content);

        return new Message()
        {
            Id = id,
            From = from,
            Chat = msg.Chat,
            Type = msg.Type,
            ReplyTo = Parse(client, msg.ReplyToMessage),
            Audio = msg.Audio,
            Document = msg.Document,
            Photo = msg.Photo,
            Command = cmd,
            Client = client,
            Content = content,
        };

    }
}
