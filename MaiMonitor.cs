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
using static TelegramBot.ChartHelper;

namespace TelegramBot
{
    public struct DateTimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Contains(DateTime dt) => (dt - Start).Seconds >= 0 && (dt - End).Seconds < 0;
        public static DateTimeRange[] Create(int minute)
        {
            var count = 1440 / minute;
            var range = new List<DateTimeRange>();

            var day = DateTime.Today;

            for (var i = 0; i < count; i++)
                range.Add(new DateTimeRange()
                {
                    Start = day,
                    End = day.AddMinutes(minute)
                });
            return range.ToArray();
        }
    }
    internal static partial class MaiMonitor
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
        public class SkipLog
        {
            public DateTime Timestamp { get; set; }
            public bool IsSkip { get; set; }
            public double LastSkipRate { get; set; }
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
        public static List<SkipLog> CompressSkipLogs = new();
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
                        //CompressSkipLogs.Clear();
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
                    var lastSkip = GetAvgSkipRate()[0] is double.NaN ? 0 : GetAvgSkipRate()[0];
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
            var now = DateTime.Now;

            var _5minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 5);
            var _10minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 10);
            var _15minLogs = CompressSkipLogs.Where(x => (now - x.Timestamp).Minutes <= 15);

            var _5minSkip = _5minLogs.Where(x => x.IsSkip);
            var _10minSkip = _10minLogs.Where(x => x.IsSkip);
            var _15minSkip = _15minLogs.Where(x => x.IsSkip);

            _5min = _5minSkip.Count() / (double)_5minLogs.Count();
            _10min = _10minSkip.Count() / (double)_10minLogs.Count();
            _15min = _15minSkip.Count() / (double)_15minLogs.Count();

            //if (count >= 60)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 60))
            //                                       .Where(x => x.IsSkip && (now - x.Timestamp).Minutes <= 5)
            //                                       .Count();
            //    _5min = skipCount / 60;
            //}
            //if (count >= 120)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 120))
            //                                       .Where(x => x.IsSkip)
            //                                       .Count();
            //    _10min = skipCount / 120;
            //}
            //if (count >= 180)
            //{
            //    double skipCount = CompressSkipLogs.Skip(Math.Max(0, count - 180))
            //                                       .Where(x => x.IsSkip)
            //                                       .Count();
            //    _15min = skipCount / 180;
            //}

            return new double[] { _5min, _10min , _15min};
        }
        static long TCPing(string host,int port)
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
    internal static partial class MaiMonitor
    {
        /// <summary>
        /// 用于获取指定尺度的全天K线图
        /// </summary>
        /// <param name="minute"></param>
        public static void CreateGraph(int minute)
        {
            var range = DateTimeRange.Create(minute);
        }
        public static void CreateGraph(DateTimeRange[] range)
        {
            var xSamples = range.Select(x => x.Start.ToString("HH:mm")).ToArray();
            List<KNode> nodes = new();

            foreach (var item in range)
                nodes.Add(CreateNode(item, CompressSkipLogs.ToArray()));

            var ySamples = CreateYSamples(nodes);
        }
        static KNode CreateNode(DateTimeRange range, SkipLog[] samples)
        {
            double max = 0;
            double min = 1;
            double open = 0;
            double close = 0;

            var matched = samples.Where(x => range.Contains(x.Timestamp))
                                 .OrderBy(x => x.Timestamp);

            if (matched.Count() == 0)
                return new KNode()
                {
                    High = 0,
                    Low = 0,
                    Close = 0,
                    Open = 0,
                    Date = range.Start
                };

            foreach (var x in matched)
            {
                max = Math.Max(x.LastSkipRate, max);
                min = Math.Min(x.LastSkipRate, min);
            }
            open = matched.First().LastSkipRate;
            close = matched.Last().LastSkipRate;

            return new KNode()
            {
                High = (float)max,
                Low = (float)min,
                Open = (float)open,
                Close = (float)close,
                Date = range.Start
            };


        }
        static IList<float> CreateYSamples(IList<KNode> nodes)
        {
            List<float> samples = new();
            var maxValue = (((int)(nodes.Select(x => x.High).Max() * 100) / 5) + 1)  * 5 / 100f;

            for (float i = maxValue;i >=0;)
            {
                samples.Add(i);
                i -= 0.05f;
            }

            return samples;
        }
    }
}
