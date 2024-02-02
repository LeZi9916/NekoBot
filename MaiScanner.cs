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

namespace TelegramBot
{
    internal class MaiScanner
    {
        static int userId = 1000;
        static string AppPath = Environment.CurrentDirectory;
        static int SearchInterval = 0;
        static string DatabasePath = Path.Combine(AppPath, "Database");
        static void Init()
        {
            //if (args.Length == 2)
            //    SearchInterval = int.Parse(args[1]);
            Aqua.RePostCount = 10;
            Aqua.Timeout = 1000;
            List<Task> taskList = new();
            for (; userId <= 1199; userId++)
                taskList.Add(GetUser(userId * 10000, userId * 10000 + 9999));

            Console.WriteLine("[Info]Finished");
            Console.ReadKey();
        }
        static async Task GetUser(int startIndex, int endIndex)
        {
            int targetUserId = startIndex;
            List<Account> accounts = Load<List<Account>>($"{startIndex / 10000}xxxx.data");
            List<int> failureList = Load<List<int>>($"failure_{startIndex / 10000}xxxx.data");

            if (accounts.Count != 0)
                targetUserId = accounts.Select(x => x.userId).OrderBy(x => x).Last() + 1;
            Console.Out.WriteLineAsync($"Range:{startIndex / 10000}\n" +
                                       $"Start at:{targetUserId}\n" +
                                       $"Scanned count:{accounts.Count}\n" +
                                       $"Failure count:{failureList.Count}\n");

            for (; targetUserId <= endIndex; targetUserId++)
            {
                var request = new Request<UserPreviewRequest>();
                request.Object.userId = targetUserId;

                var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                {
                    Console.Out.WriteLineAsync("Failure :\n" +
                    $"UserId: {targetUserId}\n" +
                                               $"StatusCode: {response.StatusCode}\n");
                    failureList.Add(targetUserId);
                    continue;
                }

                var account = new Account();
                account.userName = StringHandle(response.userName);
                account.playerRating = response.playerRating ?? 0;
                account.userId = targetUserId;
                accounts.Add(account);
                Console.Out.WriteLineAsync("Find new account:\n" +
                                    $"Username: {account.userName}\n" +
                                    $"UserId: {account.userId}\n" +
                                    $"Rating: {account.playerRating}\n" +
                                    $"StatusCode: {response.StatusCode}\n");
                Save(accounts, $"{startIndex / 10000}xxxx.data");
                Save(failureList, $"failure_{startIndex / 10000}xxxx.data");
                Thread.Sleep(SearchInterval);
            }

            for (; failureList.Count != 0;)
            {
                var _failureList = new List<int>(failureList);
                foreach (var userid in _failureList)
                {
                    failureList.Remove(userid);
                    var request = new Request<UserPreviewRequest>();
                    request.Object.userId = userid;

                    var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

                    if (response.StatusCode is not System.Net.HttpStatusCode.OK)
                    {
                        Console.Out.WriteLineAsync("Failure :\n" +
                                                  $"UserId: {userid}\n" +
                                                  $"StatusCode: {response.StatusCode}\n");
                        failureList.Add(userid);
                        continue;
                    }

                    var account = new Account();
                    account.userName = StringHandle(response.userName);
                    account.playerRating = response.playerRating ?? 0;
                    account.userId = userid;
                    accounts.Add(account);
                    Console.Out.WriteLineAsync("Find new account:\n" +
                                        $"Username: {account.userName}\n" +
                                        $"UserId: {account.userId}\n" +
                                        $"Rating: {account.playerRating}\n" +
                                        $"StatusCode: {response.StatusCode}\n");

                    Save(accounts, $"{startIndex / 10000}xxxx.data");
                    Save(failureList, $"failure_{startIndex / 10000}xxxx.data");
                    Thread.Sleep(SearchInterval);
                }

                continue;
            }
            var result = accounts.GroupBy(x => x.userId)
                                 .Select(x => x.First())
                                 .OrderBy(x => x.userId)
                                 .ToList();
            Save(result, $"{startIndex / 10000}xxxx.data");
        }
        static void Save<T>(T obj, string fileName)
        {
            var json = ToJsonString(obj);
            File.WriteAllText(Path.Combine(DatabasePath, fileName), json);
        }
        static T Load<T>(string fileName) where T : new()
        {
            if (File.Exists(Path.Combine(DatabasePath, fileName)))
                return FromJsonString<T>(File.ReadAllText(Path.Combine(DatabasePath, fileName)));
            else
                return new T();
        }
        static string StringHandle(string s)
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
        class Account
        {
            public string userName { get; set; }
            public long playerRating { get; set; }
            public int userId { get; set; }

        }
    }
}
