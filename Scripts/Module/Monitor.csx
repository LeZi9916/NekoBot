using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CZGL.SystemInfo;
using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;

public class Monitor : Destroyable, IExtension, IDestroyable, IMonitor
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "Monitor",
        Version = new Version("1.1"),
        Type = ExtensionType.Module
    };

    public int ProcessorCount = 0;
    public int CPULoad = 0;
    public int _5CPULoad = 0;
    public int _10CPULoad = 0;
    public int _15CPULoad = 0;
    List<double> CPULoadHistory = new(900);

    public long TotalMemory = 0;
    public long FreeMemory = 0;
    public long UsedMemory = 0;
    public override void Init()
    {
        ProcessorCount = SystemPlatformInfo.ProcessorCount;
        Proc();
    }
    public ExpandoObject GetReport()
    {
        dynamic report = new ExpandoObject();
        report.ProcessorCount = ProcessorCount;
        report.CPULoad = CPULoad;
        report.FreeMemory = FreeMemory;
        report.UsedMemory = UsedMemory;
        report.TotalMemory = TotalMemory;
        report._5CPULoad = _5CPULoad;
        report._10CPULoad = _10CPULoad;
        report._15CPULoad = _15CPULoad;

        return report;
    }
    async void Proc()
    {
        while(!isDestroying.IsCancellationRequested)
        {
            try
            {
                var token = isDestroying.Token;
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    await CalCPULoad();
                    CalMemInfo();
                }
            }
            catch
            { }
        }
    }
    public override void Destroy()
    {
        isDestroying.Cancel();
    }
    void CalCPULoadHistory()
    {
        var count = CPULoadHistory.Count;
        var _5minLoads = CPULoadHistory.Skip(Math.Max(0, count - 300)).Sum();
        var _10minLoads = CPULoadHistory.Skip(Math.Max(0, count - 600)).Sum();
        var _15minLoads = CPULoadHistory.Skip(Math.Max(0, count - 900)).Sum();

        _5CPULoad = (int)(_5minLoads / 300 * 100);
        _10CPULoad = (int)(_10minLoads / 600 * 100);
        _15CPULoad = (int)(_15minLoads / 900 * 100);
    }
    async Task CalCPULoad()
    {
        var a = CPUHelper.GetCPUTime();
        try
        {
            await Task.Delay(1000);
            var b = CPUHelper.GetCPUTime();
            var value = CPUHelper.CalculateCPULoad(a, b);
            CPULoad = (int)(value * 100);
            CPULoadHistory.Add(value);
            CalCPULoadHistory();
        }
        catch (Exception e)
        {
            Core.Debug(DebugType.Error, $"Failure to get processor info : \n{e.Message}");
        }
    }
    void CalMemInfo()
    {
        try
        {
            var memory = MemoryHelper.GetMemoryValue();
            TotalMemory = (long)memory.TotalPhysicalMemory;
            FreeMemory = (long)memory.AvailablePhysicalMemory;
            UsedMemory = (long)memory.UsedPhysicalMemory;
        }
        catch (Exception e)
        {
            Core.Debug(DebugType.Error, $"Failure to get memory info : \n{e.Message}");
        }
    }
    
}
