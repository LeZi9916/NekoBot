using NekoBot.Interfaces;
using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Version = NekoBot.Types.Version;
using AquaTools.Requests;
using AquaTools.Responses;
using AquaTools;

public class MaiMonitor : Destroyable, IExtension, IDestroyable, IMonitor<Dictionary<string, string>>
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "MaiMonitor",
        Version = new Version() { Major = 1, Minor = 0 },
        Type = ExtensionType.Module
    };
    public long TitleServerDelay = -1;// Title Server
    public long OAuthServerDelay = -1;// WeChat QRCode
    public long NetServerDelay = -1;//   DX Net
    public long MainServerDelay = -1;//  Main Server

    public List<PingResult> PingLogs = new();

    public long TotalRequestCount = 0;
    public long TimeoutRequestCount = 0;
    public long OtherErrorCount = 0;
    public double CompressSkipRate = 0;// 跳过率
    public long CompressSkipRequestCount = 0;
    public List<SkipLog> CompressSkipLogs = new();
    public HttpStatusCode LastResponseStatusCode;

    static Mutex mutex = new Mutex();

    public override void Init()
    {
        Proc();
    }
    public override void Destroy()
    {
        isDestroying.Cancel();
        base.Destroy();
        
    }
    async void Proc()
    {
        var token = isDestroying.Token;
        await Task.Run(() =>
        {
            while (!isDestroying.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (DateTime.Today.AddHours(4) <= DateTime.Now && DateTime.Now <= DateTime.Today.AddHours(9))
                    {
                        TitleServerDelay = -1;
                        OAuthServerDelay = -1;
                        NetServerDelay = -1;
                        MainServerDelay = -1;
                        TotalRequestCount = 0;
                        TimeoutRequestCount = 0;
                        CompressSkipRate = 0;
                        OtherErrorCount = 0;
                        CompressSkipRequestCount = 0;
                        //CompressSkipLogs.Clear();
                        PingLogs.Clear();
                        continue;
                    }

                    var req = new Request<UserRegionRequest>();
                    req.Object.userId = 11015484;
                    var response = Aqua.Post<UserRegionRequest, BaseResponse>(req);
                    LastResponseStatusCode = response.Object.StatusCode;
                    TotalRequestCount++;
                    Task.Run(() =>
                    {
                        mutex.WaitOne();
                        TitleServerDelay = TCPing(MaiServer.URL.Title, 42081);
                        token.ThrowIfCancellationRequested();
                        OAuthServerDelay = TCPing(MaiServer.URL.OAuth, 443);
                        token.ThrowIfCancellationRequested();
                        NetServerDelay = TCPing(MaiServer.URL.Net, 443);
                        token.ThrowIfCancellationRequested();
                        MainServerDelay = TCPing(MaiServer.URL.Main, 80);
                        token.ThrowIfCancellationRequested();

                        PingLogs.Add(new PingResult() { Type = ServerType.Title, Delay = TitleServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.OAuth, Delay = OAuthServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Net, Delay = NetServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Main, Delay = MainServerDelay });
                        mutex.ReleaseMutex();
                    });

                    token.ThrowIfCancellationRequested();

                    var lastSkip = GetAvgSkipRate()[0];
                    if (LastResponseStatusCode == HttpStatusCode.GatewayTimeout)
                        TimeoutRequestCount++;
                    else if (LastResponseStatusCode is HttpStatusCode.OK)
                    {
                        CompressSkipRequestCount++;
                        CompressSkipLogs.Add(new SkipLog()
                        {
                            Timestamp = DateTime.Now,
                            IsSkip = response.Object.CompressSkip,
                            LastSkipRate = lastSkip
                        });
                    }
                    else
                        OtherErrorCount++;

                    CompressSkipRate = (double)CompressSkipRequestCount / (TotalRequestCount - TimeoutRequestCount - OtherErrorCount);

                    CompressSkipLogs = CompressSkipLogs.Where(x => (DateTime.Now - x.Timestamp).Minutes <= 90).ToList();

                    Thread.Sleep(5000);
                }
                catch { }
            }
        });
    }
    public Dictionary<string,string> GetResult()
    {
        var tAvgPing = GetAvgPing(ServerType.Title);
        var oAvgPing = GetAvgPing(ServerType.OAuth);
        var nAvgPing = GetAvgPing(ServerType.Net);
        var mAvgPing = GetAvgPing(ServerType.Main);
        var skipRate = GetAvgSkipRate();
        return new()
        {
            { "tAvgPing", $"{TitleServerDelay}"},
            { "tAvgPing1", $"{tAvgPing[0]}"},
            { "tAvgPing2", $"{tAvgPing[1]}"},
            { "tAvgPing3", $"{tAvgPing[2]}"},
            { "oAvgPing", $"{OAuthServerDelay}"},
            { "oAvgPing1", $"{oAvgPing[0]}"},
            { "oAvgPing2", $"{oAvgPing[1]}"},
            { "oAvgPing3", $"{oAvgPing[2]}"},
            { "nAvgPing", $"{NetServerDelay}"},
            { "nAvgPing1", $"{nAvgPing[0]}"},
            { "nAvgPing2", $"{nAvgPing[1]}"},
            { "nAvgPing3", $"{nAvgPing[2]}"},
            { "mAvgPing", $"{MainServerDelay}"},
            { "mAvgPing1", $"{mAvgPing[0]}"},
            { "mAvgPing2", $"{mAvgPing[1]}"},
            { "mAvgPing3", $"{mAvgPing[2]}"},
            { "totalRequestCount", $"{TotalRequestCount}"},
            { "timeoutRequestCount", $"{TimeoutRequestCount}"},
            { "otherErrorCount", $"{OtherErrorCount}"},
            { "compressSkipRequestCount", $"{TotalRequestCount}"},
            { "skipRate1", $"{Math.Round(skipRate[0] * 100, 2)}"},
            { "skipRate2", $"{Math.Round(skipRate[1] * 100, 2)}"},
            { "skipRate3", $"{Math.Round(skipRate[2] * 100, 2)}"},
            { "statusCode", $"{LastResponseStatusCode}"},
        };
    }

    public long[] GetAvgPing(ServerType type)
    {
        long _5min = -1;
        long _10min = -1;
        long _15min = -1;

        var result = PingLogs.Where(x => x.Type == type).Select(x => x.Delay);
        var count = result.Count();
        if (count >= 60)
            _5min = result.Skip(Math.Max(0, count - 60)).Sum() / 60;
        if (count >= 120)
            _10min = result.Skip(Math.Max(0, count - 120)).Sum() / 120;
        if (count >= 180)
            _15min = result.Skip(Math.Max(0, count - 180)).Sum() / 180;

        return new long[] { _5min, _10min, _15min };
    }
    public double[] GetAvgSkipRate()
    {
        double _30min = double.NaN;
        double _60min = double.NaN;
        double _90min = double.NaN;
        var count = CompressSkipLogs.Count;
        var now = DateTime.Now;

        var _30minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 30);
        var _60minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 60);
        var _90minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 90);

        var _30minSkip = _30minLogs.Where(x => x.IsSkip);
        var _60minSkip = _60minLogs.Where(x => x.IsSkip);
        var _90minSkip = _90minLogs.Where(x => x.IsSkip);

        _30min = _30minSkip.Count() / (double)_30minLogs.Count();
        _60min = _60minSkip.Count() / (double)_60minLogs.Count();
        _90min = _90minSkip.Count() / (double)_90minLogs.Count();

        return new double[] { _30min, _60min, _90min };
    }
    long TCPing(string host, int port)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            TcpClient client = new TcpClient();
            client.SendTimeout = 2000;
            client.ReceiveTimeout = 2000;
            client.Connect(host, port);
            client.Close();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        catch
        {
            return -1;
        }
        finally
        {
            stopwatch.Stop();
        }

    }
}
public enum ServerType
{
    Title,
    OAuth,
    Net,
    Main
}
public class PingResult
{
    public ServerType Type { get; set; }
    public long Delay { get; set; }
}
public class SkipLog
{
    public DateTime Timestamp { get; set; }
    public bool IsSkip { get; set; }
    public double LastSkipRate { get; set; }
}