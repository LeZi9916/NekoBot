using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CZGL.SystemInfo;
using TelegramBot;

namespace NekoBot
{
    public static class Monitor
    {
        public static int ProcessorCount = 0;
        public static int CPULoad = 0;
        public static int _5CPULoad = 0;
        public static int _10CPULoad = 0;
        public static int _15CPULoad = 0;
        static List<double> CPULoadHistory = new(900);

        public static long TotalMemory = 0;
        public static long FreeMemory = 0;
        public static long UsedMemory = 0;

        static void CalCPULoadHistory()
        {
            var count = CPULoadHistory.Count;
            var _5minLoads = CPULoadHistory.Skip(Math.Max(0, count - 300)).Sum();
            var _10minLoads = CPULoadHistory.Skip(Math.Max(0, count - 600)).Sum();
            var _15minLoads = CPULoadHistory.Skip(Math.Max(0, count - 900)).Sum();

            _5CPULoad = (int)(_5minLoads / 300 * 100);
            _10CPULoad = (int)(_10minLoads / 600 * 100);
            _15CPULoad = (int)(_15minLoads / 900 * 100);
        }
        public async static void CalCPULoad()
        {
            var a = CPUHelper.GetCPUTime();
            try
            {
                while (true)
                {
                    await Task.Delay(1000);
                    var b = CPUHelper.GetCPUTime();
                    var value = CPUHelper.CalculateCPULoad(a, b);
                    a = b;
                    CPULoad = (int)(value * 100);
                    CPULoadHistory.Add(value);
                    CalCPULoadHistory();
                }
            }
            catch (Exception e)
            {
                Core.Debug(DebugType.Error, $"Failure to get processor info : \n{e.Message}");
            }
        }
        public async static void CalMemInfo()
        {
            await Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var memory = MemoryHelper.GetMemoryValue();

                        TotalMemory = (long)memory.TotalPhysicalMemory;
                        FreeMemory = (long)memory.AvailablePhysicalMemory;
                        UsedMemory = (long)memory.UsedPhysicalMemory;
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception e)
                {
                    Core.Debug(DebugType.Error, $"Failure to get memory info : \n{e.Message}");
                }
            });
        }
        public static void Init()
        {
            ProcessorCount = SystemPlatformInfo.ProcessorCount;
            CalCPULoad();
            CalMemInfo();
        }
    }
}
