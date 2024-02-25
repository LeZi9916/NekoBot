using AquaTools;
using AquaTools.Requests;
using AquaTools.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot
{
    internal static class MaiMonitor
    {
        public enum ServerType
        {
            Title,
            OAuth,
            Net,
            Main
        }
        public class PingResult
        {
            public ServerType Type {  get; set; }
            public long Delay { get; set; }
        }
        public static bool ServiceAvailability = true;

        public static long FaultInterval = -1;//平均故障间隔
        static List<long> FaultIntervalList = new();
        static DateTime LastFailureTime;

        public static long TitleServerDelay = -1;// Title Server
        public static long OAuthServerDelay = -1;// WeChat QRCode
        public static long NetServerDelay = -1;//   DX Net
        public static long MainServerDelay = -1;//  Main Server

        public static List<PingResult> PingLogs = new();

        public static long TotalRequestCount = 0;
        public static long TimeoutRequestCount = 0;
        public static long OtherErrorCount = 0;
        public static double CompressSkipRate = 0;// 跳过率
        public static long CompressSkipRequestCount = 0;
        public static List<long> CompressSkipLogs = new();
        public static HttpStatusCode LastResponseStatusCode;

        const string TitleServer = "maimai-gm.wahlap.com";
        const string OAuthServer = "tgk-wcaime.wahlap.com";
        const string NetServer = "maimai.wahlap.com";
        const string MainServer = "ai.sys-all.cn";

        static Mutex mutex = new Mutex();

        public static void Init()
        {
            LastFailureTime = DateTime.Now;
            FaultIntervalList = Config.Load<List<long>>(Path.Combine(Config.DatabasePath, "FaultIntervalList.data"));
            if (File.Exists(Path.Combine(Config.DatabasePath, "LastFailureTime.data")))
                LastFailureTime = Config.Load<DateTime>(Path.Combine(Config.DatabasePath, "LastFailureTime.data"));

            Proc();            
        }
        static async void Proc()
        {
            await Task.Run(() => 
            {
                for(; ; )
                {
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
                        CompressSkipLogs.Clear();
                        PingLogs.Clear();
                        continue;
                    }

                    var response = Aqua.TestPostAsync(Config.keyChips[0]).Result;
                    LastResponseStatusCode = response.Object.StatusCode;
                    TotalRequestCount++;
                    Task.Run(() => 
                    {
                        mutex.WaitOne();
                        TitleServerDelay = TCPing(TitleServer, 42081);
                        OAuthServerDelay = TCPing(OAuthServer, 443);
                        NetServerDelay = TCPing(NetServer, 443);
                        MainServerDelay = TCPing(MainServer, 80);


                        PingLogs.Add(new PingResult() { Type = ServerType.Title, Delay = TitleServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.OAuth, Delay = OAuthServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Net, Delay = NetServerDelay });
                        PingLogs.Add(new PingResult() { Type = ServerType.Main, Delay = MainServerDelay });
                        mutex.ReleaseMutex();
                    });

                    if (TitleServerDelay == -1 || OAuthServerDelay == -1 || NetServerDelay == -1 || MainServerDelay == -1)
                    {
                        if (ServiceAvailability)
                            FaultIntervalList.Add((long)(LastFailureTime - DateTime.Now).TotalSeconds);
                        ServiceAvailability = false;
                    }
                    else
                    {
                        if (!ServiceAvailability)
                            LastFailureTime = DateTime.Now;
                        ServiceAvailability = true;
                    }

                    if (LastResponseStatusCode == HttpStatusCode.GatewayTimeout)
                        TimeoutRequestCount++;
                    else if(LastResponseStatusCode != HttpStatusCode.OK)
                        OtherErrorCount++;
                    if (response.Object.CompressSkip)
                    {
                        CompressSkipRequestCount++;
                        CompressSkipLogs.Add(1);
                    }
                    else
                        CompressSkipLogs.Add(0);

                    CompressSkipRate = (double)CompressSkipRequestCount / (TotalRequestCount - TimeoutRequestCount - OtherErrorCount);
                    if (FaultIntervalList.Count != 0)
                        FaultInterval = FaultIntervalList.Sum() / FaultIntervalList.Count();

                    Config.Save(Path.Combine(Config.DatabasePath, "FaultIntervalList.data"), FaultIntervalList,false);
                    Config.Save(Path.Combine(Config.DatabasePath, "LastFailureTime.data"), LastFailureTime,false);
                    

                    Thread.Sleep(5000);
                }
            });
        }
        public static long[] GetAvgPing(ServerType type)
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

            return new long[] { _5min, _10min , _15min};
        }
        public static double[] GetAvgSkipRate()
        {
            double _5min = double.NaN;
            double _10min = double.NaN;
            double _15min = double.NaN;
            var count = CompressSkipLogs.Count;             

            if(count >= 60)
            {
                double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 60)).Where(x => x == 1).Sum();
                _5min = skipCount / 60;
            }
            if (count >= 120)
            {
                double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 120)).Where(x => x == 1).Sum();
                _10min = skipCount / 120;
            }
            if (count >= 180)
            {
                double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 180)).Where(x => x == 1).Sum();
                _15min = skipCount / 180;
            }

            return new double[] { _5min, _10min , _15min};
        }
        static long TCPing(string host,int port)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            for(int i = 0;i<5;i++)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(host, port);
                    client.Close();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                }
                catch
                {
                    continue;
                }
            }
            stopwatch.Stop();
            return -1;
        }
        
    }
}
