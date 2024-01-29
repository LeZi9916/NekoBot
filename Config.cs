using AquaTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot
{
    enum Permission
    {
        Unknow = -1,
        Ban,
        Common,
        Advanced,
        Admin,
        Root
    }
    class TUser
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name
        {
            get => FirstName + " " + LastName;
        }
        public Permission Level { get; set; } = Permission.Common;
        public string MaiUserId { get; set; } = null;

        public bool CheckPermission(Permission targetLevel) => Level >= targetLevel ? true : false;
        public void SetPermission(Permission targetLevel)
        {
            Level = targetLevel;
            Config.SaveData();
        }
    }
    internal static class Config
    {
        static DateTime Up = DateTime.Now;
        static string AppPath = Environment.CurrentDirectory;
        static string LogsPath = Path.Combine(AppPath, "logs");
        static string DatabasePath = Path.Combine(AppPath, "Database");
        public static string TempPath = Path.Combine(AppPath, "Temp");
        static string LogFile = Path.Combine(LogsPath,$"{Up.ToString("yyyy-MM-dd HH-mm-ss")}.log");

        public static bool EnableAutoSave = true;
        public static int AutoSaveInterval = 300000;

        public static long TotalHandleCount = 0;
        public static List<long> TimeSpentList = new();

        static Mutex mutex = new();

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
        {new KeyChip()
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
            AutoSave();
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
                    Program.Debug(DebugType.Info, "Auto save data start");
                    SaveData();
                }
            });
        }
        public static void SaveData()
        {
            Save(Path.Combine(DatabasePath, "UserList.data"), TUserList);
            Save(Path.Combine(DatabasePath, "UserIdList.data"), UserIdList);
            Save(Path.Combine(DatabasePath, "TotalHandleCount.data"), TotalHandleCount);
            Save(Path.Combine(DatabasePath, "TimeSpentList.data"), TimeSpentList);
        }
        public static void WriteLog(string s)
        {
            mutex.WaitOne();
            File.AppendAllText(LogFile,$"{s}\n",Encoding.UTF8);
            mutex.ReleaseMutex();
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
        public static void Save<T>(string path,T target)
        {
            try
            {
                var fileStream = File.Open(path, FileMode.Create);
                fileStream.Write(Encoding.UTF8.GetBytes(ToJsonString(target)));
                fileStream.Close();
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

                Program.Debug(DebugType.Info, $"Loaded File: {path}\n");
                return result;
            }
            catch (Exception e) 
            {
                Program.Debug(DebugType.Error, $"Loading \"{path}\" Failure:\n{e.Message}\n");
                return new T();
            }
        }
        public static string ToJsonString<T>(T target) => JsonSerializer.Serialize(target);
        public static T FromJsonString<T>(string json) => JsonSerializer.Deserialize<T>(json);
    }
}
