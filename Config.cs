using AquaTools;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static TelegramBot.MaiScanner;
using static TelegramBot.MaiDatabase;
using File = System.IO.File;
using TelegramBot.Class;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace TelegramBot
{    
    
    
    
    internal static class Config
    {
        static DateTime Up = DateTime.Now;
        static string AppPath = Environment.CurrentDirectory;
        internal static string LogsPath = Path.Combine(AppPath, "logs");
        internal static string DatabasePath = Path.Combine(AppPath, "Database");
        internal static string TempPath = Path.Combine(AppPath, "Temp");
        internal static string LogFile = Path.Combine(LogsPath,$"{Up.ToString("yyyy-MM-dd HH-mm-ss")}.log");
        internal static HotpAuthenticator Authenticator = new HotpAuthenticator();

        internal static bool EnableAutoSave = true;
        internal static int AutoSaveInterval = 900000;

        internal static long TotalHandleCount = 0;
        internal static List<long> TimeSpentList = new();

        internal static List<long> GroupIdList = new();
        internal static List<Group> GroupList = new();


        internal static List<long> UserIdList = new() { 1136680302 };
        internal static List<TUser> TUserList = new()
        {
            new TUser()
            {
                Id = 1136680302,
                FirstName = "Tanuoxi",
                LastName = null,
                Level = Permission.Root
            }
        };        

        internal static List<KeyChip> keyChips = new() 
        {
            new KeyChip()
            {
                PlaceId = 2120,
                PlaceName = "SUPER101潮漫北流店",
                RegionId = 28,
                RegionName = "广西",
                KeyChipId = "A63E-01E14596415"
            },
            new KeyChip() 
            {
                PlaceId = 1,
                PlaceName = "Unknow",
                RegionId = 1,
                RegionName = "Unknow",
                KeyChipId = "A63E-01E14150010"
            }
        };

        public static void Init()
        {
            Check();
            if (File.Exists(Path.Combine(DatabasePath, "UserList.data")))
                TUserList = Load<List<TUser>>(Path.Combine(DatabasePath, "UserList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "UserIdList.data")))
                UserIdList = Load<List<long>>(Path.Combine(DatabasePath, "UserIdList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "TotalHandleCount.data")))
                TotalHandleCount = Load<long>(Path.Combine(DatabasePath, "TotalHandleCount.data"));
            if (File.Exists(Path.Combine(DatabasePath, "TimeSpentList.data")))
                TimeSpentList = Load<List<long>>(Path.Combine(DatabasePath, "TimeSpentList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "GroupList.data")))
                GroupList = Load<List<Group>>(Path.Combine(DatabasePath, "GroupList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "GroupIdList.data")))
                GroupIdList = Load<List<long>>(Path.Combine(DatabasePath, "GroupIdList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "MaiAccountList.data")))
                MaiAccountList = Load<List<MaiAccount>>(Path.Combine(DatabasePath, "MaiAccountList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "MaiInvalidUserIdList.data")))
                MaiInvaildUserIdList = Load<List<int>>(Path.Combine(DatabasePath, "MaiInvalidUserIdList.data"));
            if (File.Exists(Path.Combine(DatabasePath, "HotpAuthenticator.data")))
                Authenticator = Load<HotpAuthenticator>(Path.Combine(DatabasePath, "HotpAuthenticator.data"));
            if (File.Exists(Path.Combine(DatabasePath, "token.config")))
                Program.Token = File.ReadAllText(Path.Combine(DatabasePath, "token.config"));
            else
            {
                Program.Debug(DebugType.Error, "Config file isn't exist");
                Environment.Exit(-1);
            }
            MaiDataInit();
            AutoSave();
        }
        public static Group SearchGroup(long groupId)
        {
            var result = GroupList.Where(u => u.GroupId == groupId).ToArray();

            return result.Length == 0 ? null : result[0];
        }
        public static TUser SearchUser(long userId)
        {
            var result = TUserList.Where(u => u.Id == userId).ToArray();

            return result.Length == 0 ? null: result[0];
        }
        public static void AddUser(TUser user)
        {
            TUserList.Add(user);
            UserIdList.Add(user.Id);
            Save(Path.Combine(DatabasePath, "UserList.data"),TUserList);
            Save(Path.Combine(DatabasePath, "UserIdList.data"),UserIdList);
        }
        public static async void AutoSave()
        {
            await Task.Run(() => 
            {
                while(true)
                {
                    Thread.Sleep(AutoSaveInterval);
                    if (!EnableAutoSave)
                        break;
                    Program.Debug(DebugType.Debug, "Auto save data start");
                    SaveData();
                }
            });
        }
        public static async void SaveData()
        {
            Save(Path.Combine(DatabasePath, "UserList.data"), TUserList);
            Save(Path.Combine(DatabasePath, "UserIdList.data"), UserIdList);
            Save(Path.Combine(DatabasePath, "TotalHandleCount.data"), TotalHandleCount);
            Save(Path.Combine(DatabasePath, "TimeSpentList.data"), TimeSpentList);
            Save(Path.Combine(DatabasePath, "GroupList.data"), GroupList);
            Save(Path.Combine(DatabasePath, "GroupIdList.data"), GroupIdList);
            Save(Path.Combine(DatabasePath, "MaiAccountList.data"), MaiAccountList);
            Save(Path.Combine(DatabasePath, "MaiInvalidUserIdList.data"), MaiInvaildUserIdList);
            Save(Path.Combine(DatabasePath, "HotpAuthenticator.data"), Authenticator);
            Program.BotCommands = await Program.botClient.GetMyCommandsAsync();
        }
        
        static void Check()
        {
            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
        }        


        public static async void Save<T>(string path,T target,bool debugMessage = true)
        {
            try
            {
                var fileStream = File.Open(path, FileMode.Create);
                await fileStream.WriteAsync(Encoding.UTF8.GetBytes(ToJsonString(target)));
                fileStream.Close();
                if (debugMessage)
                    Program.Debug(DebugType.Info, $"Saved File {path}");
            }
            catch(Exception e)
            {
                
                Program.Debug(DebugType.Error, $"Saving File \"{path}\" Failure:\n{e.Message}");
            }
        }
        public static T Load<T>(string path) where T : new()
        {
            try
            {
                var json = File.ReadAllText(path);
                var result = FromJsonString<T>(json);

                Program.Debug(DebugType.Info, $"Loaded File: {path}");
                return result;
            }
            catch (Exception e) 
            {
                Program.Debug(DebugType.Error, $"Loading \"{path}\" Failure:\n{e.Message}");
                return new T();
            }
        }
        public static string ToJsonString<T>(T target) => JsonSerializer.Serialize(target);
        public static T FromJsonString<T>(string json) => JsonSerializer.Deserialize<T>(json);
    }
}
