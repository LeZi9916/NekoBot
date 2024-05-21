using AquaTools.Requests;
using AquaTools.Responses;
using AquaTools;
using NekoBot.Interfaces;
using NekoBot.Types;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using NekoBot;
using Version = NekoBot.Types.Version;

public class MaiScanner : ExtensionCore, IExtension
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "MaiScanner",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Module,
        Dependencies = new ExtensionInfo[]{
                new ExtensionInfo()
                {
                    Name = "UserDatabase",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Database
                },
                new ExtensionInfo()
                {
                    Name = "GroupDatabase",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Database
                },
                new ExtensionInfo()
                {
                    Name = "MaiDatabase",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Database
                },
                new ExtensionInfo()
                {
                    Name = "MaiMonitor",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Module
                },
                new ExtensionInfo()
                {
                    Name = "JsonSerializer",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Serializer
                },
                new ExtensionInfo()
                {
                    Name = "YamlSerializer",
                    Version = new Version() { Major = 1, Minor = 0 },
                    Type = ExtensionType.Serializer
                }
            },
        SupportUpdate = new UpdateType[]
        {
                UpdateType.Message,
                UpdateType.EditedMessage
        }
    };
    static string AppPath = Environment.CurrentDirectory;
    static string DatabasePath = Path.Combine(AppPath, "Database");
    static string TempPath = Path.Combine(AppPath, "Database/ScanTempFile");


    //public int StartIndex = 1000;
    //public int EndIndex = 1199;
    //public int SearchInterval = 1000;
    //public int CurrentQps = 0;
    //public int QpsLimit = 10;

    //public long TotalAccountCount = 0;
    //public long CurrentAccountCount = 0;

    //public bool isRunning = false;

    //List<Task> task = new();
    //Mutex mutex = new();
    //Mutex mutex2 = new();
    //Task? QpsTimer = null;
    //bool isDestroying = false;
    //public CancellationTokenSource cancelSource = new CancellationTokenSource();
    //public override void Destroy()
    //{
    //    isRunning = false;
    //    isDestroying = true;

    //    if (task.Count > 0)
    //        Task.WaitAll(task.ToArray());
    //    if (QpsTimer is not null)
    //        QpsTimer.Wait();
    //}
    //public async void Start()
    //{
    //    if (QpsTimer is null)
    //    {
    //        QpsTimer = Task.Run(() =>
    //        {
    //            while (true)
    //            {
    //                if (isDestroying)
    //                    break;
    //                mutex.WaitOne();
    //                CurrentQps = 0;
    //                mutex.ReleaseMutex();
    //                Thread.Sleep(1000);
    //            }
    //        });
    //    }

    //    if (isRunning)
    //        return;

    //    Aqua.RePostCount = 10;
    //    Aqua.Timeout = 1000;
    //    isRunning = true;
    //    task.Clear();
    //    cancelSource = new CancellationTokenSource();

    //    TotalAccountCount = (EndIndex - StartIndex + 1) * 10000;

    //    await Task.Run(() =>
    //    {
    //        for (; StartIndex <= EndIndex; StartIndex++)
    //            task.Add(GetUser(StartIndex * 10000, StartIndex * 10000 + 9999));
    //        Task.WaitAll(task.ToArray());
    //        IsFinished();
    //        //Config.SaveData();
    //    });
    //}
    //public async void Update(int index = 0)
    //{
    //    if (isRunning)
    //        return;

    //    Aqua.RePostCount = 10;
    //    Aqua.Timeout = 1000;
    //    isRunning = true;
    //    task.Clear();
    //    cancelSource = new CancellationTokenSource();

    //    TotalAccountCount = database.MaiAccountList.Count;
    //    CurrentAccountCount = index;

    //    await Task.Run(() =>
    //    {
    //        var accounts = database.MaiAccountList;
    //        for (; index < accounts.Count; index++)
    //            task.Add(UpdateUser(accounts[index]));
    //        Task.WaitAll(task.ToArray());
    //        IsFinished();
    //        //Config.SaveData();
    //    });
    //}
    //async Task UpdateUser(MaiAccount account)
    //{
    //    bool canUpdate = true;
    //    while (true)
    //    {
    //        if (isDestroying)
    //            break;
    //        if (DateTime.Today.AddHours(3) <= DateTime.Now && DateTime.Now <= DateTime.Today.AddHours(9))
    //            canUpdate = false;
    //        else
    //            canUpdate = true;

    //        var request = new Request<UserPreviewRequest>();
    //        request.Object.userId = account.userId;

    //        QpsIncrease();
    //        for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20 || !canUpdate; QpsIncrease())
    //        {
    //            if (cancelSource.Token.IsCancellationRequested)
    //                break;
    //            Thread.Sleep(500);
    //        }
    //        if (cancelSource.Token.IsCancellationRequested)
    //            break;


    //        var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

    //        if (response.StatusCode is not System.Net.HttpStatusCode.OK)
    //            continue;

    //        account.playerRating = response.playerRating ?? 0;
    //        account.lastDataVersion = response.lastDataVersion;
    //        account.lastRomVersion = response.lastRomVersion;
    //        account.lastGameId = response.lastGameId;
    //        account.banState = response.banState;
    //        account.lastUpdate = DateTime.Now;
    //        CountIncrease();
    //        break;

    //    }

    //}
    //async Task GetUser(int startIndex, int endIndex)
    //{
    //    int targetUserId = startIndex;
    //    List<MaiAccount> accounts = new();
    //    List<int> failureList = new();

    //    for (; targetUserId <= endIndex; targetUserId++)
    //    {
    //        if (isDestroying)
    //            break;
    //        for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20;)
    //        {
    //            Thread.Sleep(100);
    //            continue;
    //        }

    //        var request = new Request<UserPreviewRequest>();
    //        request.Object.userId = targetUserId;

    //        var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

    //        if (response.StatusCode is not HttpStatusCode.OK)
    //        {
    //            failureList.Add(targetUserId);
    //            continue;
    //        }

    //        var account = new MaiAccount();
    //        account.userName = StringHandle(response.userName);
    //        account.playerRating = response.playerRating ?? 0;
    //        account.userId = targetUserId;
    //        account.lastDataVersion = response.lastDataVersion;
    //        account.lastRomVersion = response.lastRomVersion;
    //        account.lastGameId = response.lastGameId;
    //        account.banState = response.banState;
    //        account.lastUpdate = DateTime.Now;
    //        accounts.Add(account);
    //        CountIncrease();

    //        QpsIncrease();
    //        Thread.Sleep(SearchInterval);

    //        if (!isRunning)
    //            return;
    //    }

    //    for (; failureList.Count != 0;)
    //    {
    //        if (isDestroying)
    //            break;
    //        var _failureList = new List<int>(failureList);
    //        foreach (var userid in _failureList)
    //        {
    //            for (; CurrentQps > QpsLimit || monitor.CompressSkipRate >= 0.20;)
    //            {
    //                Thread.Sleep(100);
    //                continue;
    //            }

    //            failureList.Remove(userid);
    //            var request = new Request<UserPreviewRequest>();
    //            request.Object.userId = userid;

    //            var response = (await Aqua.PostAsync<UserPreviewRequest, UserPreviewResponse>(request)).Object;

    //            if (response.StatusCode is not System.Net.HttpStatusCode.OK)
    //            {
    //                failureList.Add(userid);
    //                continue;
    //            }

    //            var account = new MaiAccount();
    //            account.userName = StringHandle(response.userName);
    //            account.playerRating = response.playerRating ?? 0;
    //            account.userId = userid;
    //            account.lastDataVersion = response.lastDataVersion;
    //            account.lastRomVersion = response.lastRomVersion;
    //            account.lastGameId = response.lastGameId;
    //            account.banState = response.banState;
    //            account.lastUpdate = DateTime.Now;
    //            accounts.Add(account);
    //            CountIncrease();

    //            QpsIncrease();
    //            Thread.Sleep(SearchInterval);

    //            if (!isRunning)
    //                return;
    //        }

    //        continue;
    //    }

    //    var result = accounts.GroupBy(x => x.userId)
    //                         .Select(x => x.First())
    //                         .Where(x => x.userName is not null);

    //    database.MaiAccountList.AddRange(result);
    //}
    ///// <summary>
    ///// QPS递增
    ///// </summary>
    //void QpsIncrease()
    //{
    //    mutex.WaitOne();
    //    CurrentQps++;
    //    mutex.ReleaseMutex();
    //}
    ///// <summary>
    ///// 用于报告目前进度
    ///// </summary>
    //void CountIncrease()
    //{
    //    mutex2.WaitOne();
    //    CurrentAccountCount++;
    //    mutex2.ReleaseMutex();
    //}
    //bool IsFinished()
    //{
    //    var count = task.Where(x => x.IsCompleted == true).Count();
    //    if (count != task.Count)
    //    {
    //        isRunning = false;
    //        return false;
    //    }
    //    return true;
    //}
    //public static string ToJsonString<T>(T target) => Serialize(target);
    //public static T FromJsonString<T>(string json) => Deserialize<T>(json);
    //public static string? Serialize<T>(T obj)
    //{
    //    var ext = ScriptManager.GetExtension("JsonSerializer") as ISerializer;
    //    JsonSerializer? serializer = null;
    //    if (ext is not null)
    //    {
    //        serializer = ext as JsonSerializer;
    //        var options = new JsonSerializerOptions
    //        {
    //            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    //            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    //            Converters = { new DateTimeConverter() },
    //            IncludeFields = true,
    //            WriteIndented = true
    //        };
    //        var result = serializer!.Serialize<T>(obj, options);
    //        return result;
    //    }
    //    return default;

    //}
    //public static T? Deserialize<T>(string json)
    //{
    //    var ext = ScriptManager.GetExtension("JsonSerializer") as ISerializer;
    //    JsonSerializer? serializer = null;
    //    if (ext is not null)
    //    {
    //        serializer = ext as JsonSerializer;
    //        var options = new JsonSerializerOptions
    //        {
    //            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    //            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    //            Converters = { new DateTimeConverter() },
    //            IncludeFields = true
    //        };
    //        var result = serializer!.Deserialize<T>(json, options);
    //        return result;
    //    }
    //    return default;
    //}
}
