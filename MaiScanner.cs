using AquaTools.Requests;
using AquaTools.Responses;
using AquaTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using static TelegramBot.MaiDatabase;

namespace TelegramBot
{
    internal static partial class MaiScanner
    {

        static string AppPath = Environment.CurrentDirectory;
        static string DatabasePath = Path.Combine(AppPath, "Database");
        static string TempPath = Path.Combine(AppPath, "Database/ScanTempFile");


        internal static int StartIndex = 1000;
        internal static int EndIndex = 1199;
        internal static int SearchInterval = 1000;
        internal static int CurrentQps = 0;
        internal static int QpsLimit = 10;

        internal static long TotalAccountCount = 0;
        internal static long CurrentAccountCount = 0;

        internal static bool isRunning = false;

        static List<Task> task = new();
        static Mutex mutex = new();
        static Mutex mutex2 = new();
        static Task QpsTimer = Task.Run(() =>
        {
            while (true)
            {
                mutex.WaitOne();
                CurrentQps = 0;
                mutex.ReleaseMutex();
                Thread.Sleep(1000);
            }
        });
        internal static CancellationTokenSource cancelSource = new CancellationTokenSource();
        internal static async void Init()
        {
            if (isRunning)
                return;

            Aqua.RePostCount = 10;
            Aqua.Timeout = 1000;
            isRunning = true;
            task.Clear();
            cancelSource = new CancellationTokenSource();

            TotalAccountCount = (EndIndex - StartIndex + 1) * 10000;

            await Task.Run(() =>
            {
                for (; StartIndex <= EndIndex; StartIndex++)
                    task.Add(GetUser(StartIndex * 10000, StartIndex * 10000 + 9999));
                Task.WaitAll(task.ToArray());
                IsFinished();
                Config.SaveData();
            });

        }
        internal static async void Update(int index = 0)
        {
            if (isRunning)
                return;

            Aqua.RePostCount = 10;
            Aqua.Timeout = 1000;
            isRunning = true;
            task.Clear();
            cancelSource = new CancellationTokenSource();

            TotalAccountCount = MaiAccountList.Count;
            CurrentAccountCount = index;

            await Task.Run(() =>
            {
                var accounts = MaiAccountList;
                for(;index < accounts.Count;index++)
                    task.Add(UpdateUser(accounts[index]));
                Task.WaitAll(task.ToArray());
                IsFinished();
                Config.SaveData();
            });
        }
        static async Task UpdateUser(MaiAccount account)
        {
            bool canUpdate = true;
            while (true)
            {
                if (DateTime.Today.AddHours(3) <= DateTime.Now && DateTime.Now <= DateTime.Today.AddHours(9))
                    canUpdate = false;
                else
                    canUpdate = true;                

                var request = new Request<UserPreviewRequest>();
                request.Object.userId = account.userId;

                
                QpsIncrease();
                for (; CurrentQps > QpsLimit || MaiMonitor.CompressSkipRate >= 0.20 || !canUpdate; QpsIncrease())
                {
                    if (cancelSource.Token.IsCancellationRequested)
                        break;
                    Thread.Sleep(500);
                }
                if (cancelSource.Token.IsCancellationRequested)
                    break;


                var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    continue;

                account.playerRating = response.playerRating ?? 0;
                account.lastDataVersion = response.lastDataVersion;
                account.lastRomVersion = response.lastRomVersion;
                account.lastGameId = response.lastGameId;
                account.banState = response.banState;
                account.lastUpdate = DateTime.Now;
                CountIncrease();
                break;
                
            }

        }
        static async Task GetUser(int startIndex, int endIndex)
        {
            int targetUserId = startIndex;
            List<MaiAccount> accounts = new();
            List<int> failureList = new();

            for (; targetUserId <= endIndex; targetUserId++)
            {
                for (; CurrentQps > QpsLimit || MaiMonitor.CompressSkipRate >= 0.20;)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var request = new Request<UserPreviewRequest>();
                request.Object.userId = targetUserId;

                var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                {
                    failureList.Add(targetUserId);
                    continue;
                }

                var account = new MaiAccount();
                account.userName = StringHandle(response.userName);
                account.playerRating = response.playerRating ?? 0;
                account.userId = targetUserId;
                account.lastDataVersion = response.lastDataVersion;
                account.lastRomVersion = response.lastRomVersion;
                account.lastGameId = response.lastGameId;
                account.banState = response.banState;
                account.lastUpdate = DateTime.Now;
                accounts.Add(account);
                CountIncrease();

                QpsIncrease();
                Thread.Sleep(SearchInterval);

                if (!isRunning)
                    return;
            }

            for (; failureList.Count != 0;)
            {
                var _failureList = new List<int>(failureList);
                foreach (var userid in _failureList)
                {
                    for (; CurrentQps > QpsLimit || MaiMonitor.CompressSkipRate >= 0.20;)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    failureList.Remove(userid);
                    var request = new Request<UserPreviewRequest>();
                    request.Object.userId = userid;

                    var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                    if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    {
                        failureList.Add(userid);
                        continue;
                    }

                    var account = new MaiAccount();
                    account.userName = StringHandle(response.userName);
                    account.playerRating = response.playerRating ?? 0;
                    account.userId = userid;
                    account.lastDataVersion = response.lastDataVersion;
                    account.lastRomVersion = response.lastRomVersion;
                    account.lastGameId = response.lastGameId;
                    account.banState = response.banState;
                    account.lastUpdate = DateTime.Now;
                    accounts.Add(account);
                    CountIncrease();

                    QpsIncrease();
                    Thread.Sleep(SearchInterval);

                    if (!isRunning)
                        return;
                }

                continue;
            }

            var result = accounts.GroupBy(x => x.userId)
                                 .Select(x => x.First())
                                 .Where(x => x.userName is not null);

            MaiAccountList.AddRange(result);
        }
        internal static string StringHandle(string s)
        {
            if (s is null)
                return null;

            StringBuilder sb = new();
            foreach (char c in s)
            {
                if (c == '　')
                    sb.Append(' ');
                else if (c >= 0xFF01 && c <= 0xFF5E)
                    sb.Append((char)(c - 0xFEE0));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

    }
    internal static partial class MaiScanner
    {
        /// <summary>
        /// QPS递增
        /// </summary>
        static void QpsIncrease()
        {
            mutex.WaitOne();
            CurrentQps++;
            mutex.ReleaseMutex();
        }
        /// <summary>
        /// 用于报告目前进度
        /// </summary>
        static void CountIncrease()
        {
            mutex2.WaitOne();
            CurrentAccountCount++;
            mutex2.ReleaseMutex();
        }
        static bool IsFinished()
        {
            var count = task.Where(x => x.IsCompleted == true).Count();
            if (count != task.Count)
            {
                isRunning = false;
                return false;
            }
            return true;
        }

        public static string ToJsonString<T>(T target) => Serialize(target);
        public static T FromJsonString<T>(string json) => Deserialize<T>(json);
        public static string Serialize<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeConverter() },
                IncludeFields = true,
                WriteIndented = true
            };
            return JsonSerializer.Serialize(obj, options);
        }
        public static T Deserialize<T>(string json)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateTimeConverter() },
                IncludeFields = true
            };
            var result = JsonSerializer.Deserialize<T>(json, options);

            return result;
        }
        internal class MaiAccount
        {
            public string userName { get; set; }
            public long playerRating { get; set; }
            public int userId { get; set; }
            public string lastGameId { get; set; }
            public string lastDataVersion { get; set; }
            public string lastRomVersion { get; set; }
            public int? banState { get; set; }
            public DateTime lastUpdate { get; set; }

        }
    }
}
