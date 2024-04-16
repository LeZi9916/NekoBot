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
using File = System.IO.File;
using TelegramBot.Class;
using Telegram.Bot;
using Group = TelegramBot.Class.Group;

namespace TelegramBot
{
    public static class Config
    {
        static DateTime Up = DateTime.Now;
        public static string AppPath = Environment.CurrentDirectory;
        public static string LogsPath = Path.Combine(AppPath, "logs");
        public static string DatabasePath = Path.Combine(AppPath, "Database");
        public static string TempPath = Path.Combine(AppPath, "Temp");
        public static string LogFile = Path.Combine(LogsPath,$"{Up.ToString("yyyy-MM-dd HH-mm-ss")}.log");
        public static HotpAuthenticator Authenticator = new HotpAuthenticator();

        public static bool EnableAutoSave = true;
        public static int AutoSaveInterval = 900000;

        public static long TotalHandleCount = 0;
        public static List<long> TimeSpentList = new();

        public static List<long> GroupIdList = new();
        public static List<Group> GroupList = new();


        public static List<long> UserIdList = new() { 1136680302 };
        public static List<TUser> TUserList = new()
        {
            new TUser()
            {
                Id = 1136680302,
                FirstName = "Tanuoxi",
                LastName = null,
                Level = Permission.Root
            }
        };        

        public static List<KeyChip> keyChips = new() 
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
            if (File.Exists(Path.Combine(DatabasePath, "HotpAuthenticator.data")))
                Authenticator = Load<HotpAuthenticator>(Path.Combine(DatabasePath, "HotpAuthenticator.data"));  
            if (File.Exists(Path.Combine(DatabasePath, "token.config")))
                Program.Token = File.ReadAllText(Path.Combine(DatabasePath, "token.config"));
            else
            {
                Program.Debug(DebugType.Error, "Config file isn't exist");
                Environment.Exit(-1);
            }
            AutoSave();
        }
#nullable enable
        public static Group? SearchGroup(long groupId)
        {
            if (!GroupIdList.Contains(groupId))
                return null;
            var result = GroupList.Where(u => u.Id == groupId).ToArray();

            return result[0];
        }
        public static TUser? SearchUser(long userId)
        {
            if (!UserIdList.Contains(userId))
                return null;

            var result = TUserList.Where(u => u.Id == userId).ToArray();

            return result[0];
        }
#nullable disable
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
            Save(Path.Combine(DatabasePath, "HotpAuthenticator.data"), Authenticator);
            ScriptManager.Save();
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
        public static void Load<T>(string path,out T obj)
        {
            try
            {
                var json = File.ReadAllText(path);
                obj = FromJsonString<T>(json);

                Program.Debug(DebugType.Info, $"Loaded File: {path}");
                
            }
            catch (Exception e)
            {
                Program.Debug(DebugType.Error, $"Loading \"{path}\" Failure:\n{e.Message}");
                obj = default(T);
            }
        }
        public static string ToJsonString<T>(T target) => JsonSerializer.Serialize(target);
        public static T FromJsonString<T>(string json) => JsonSerializer.Deserialize<T>(json);
    }
}
