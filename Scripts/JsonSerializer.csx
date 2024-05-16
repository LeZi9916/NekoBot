using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBot.Interfaces;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;

#nullable enable
namespace TelegramBot.Scripts
{
    public class JsonSerializer : ExtensionCore, ISerializer
    {
        public string Name { get; } = "JsonSerializer";
        public static string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
        public static T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);


    }
}
