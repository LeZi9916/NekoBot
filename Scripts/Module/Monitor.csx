using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CZGL.SystemInfo;
using NekoBot;
using NekoBot.Interfaces;
using NekoBot.Types;
using Version = NekoBot.Types.Version;

public class Monitor : Destroyable, IExtension, IDestroyable, IMonitor<Dictionary<string,long>>
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "Monitor",
        Version = new Version() { Major = 1, Minor = 0 },
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
    public Dictionary<string,long> GetResult()
    {
        return new Dictionary<string, long>()
        {
            { "ProcessorCount",ProcessorCount },
            { "CPULoad",CPULoad },
            { "_5CPULoad",_5CPULoad },
            { "_10CPULoad",_10CPULoad },
            { "_15CPULoad",_15CPULoad },
            { "TotalMemory",TotalMemory},
            { "FreeMemory",FreeMemory },
            { "UsedMemory",UsedMemory },
        };
    }
    async void Proc()
    {
        var token = isDestroying.Token;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            CalCPULoad();
            CalMemInfo();
            await Task.Delay(1000);
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
    void CalCPULoad()
    {
        var a = CPUHelper.GetCPUTime();
        try
        {
            var b = CPUHelper.GetCPUTime();
            var value = CPUHelper.CalculateCPULoad(a, b);
            a = b;
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
