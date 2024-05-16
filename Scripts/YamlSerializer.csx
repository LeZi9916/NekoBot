using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBot.Interfaces;
using TelegramBot.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = TelegramBot.Interfaces.ISerializer;
using Message = TelegramBot.Types.Message;
#nullable enable
namespace TelegramBot.Scripts
{
    public class YamlSerializer: ExtensionCore, ISerializer
	{
		public string Name { get; } = "YamlSerializer";
		public static string Serialize<T>(T obj)
		{
			var serializer = new SerializerBuilder()
								 .WithNamingConvention(CamelCaseNamingConvention.Instance)
								 .Build();
			return serializer.Serialize(obj);
		}
		public static T? Deserialize<T>(string yaml) 
		{
			var deserializer = new DeserializerBuilder()
								   .WithNamingConvention(UnderscoredNamingConvention.Instance)
								   .Build();
			return deserializer.Deserialize<T>(yaml);
		}
	}
}
