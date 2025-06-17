using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NekoBot;

public static class BotLogger
{

    static Task _logWritebackLoopTask = Task.CompletedTask;

    static readonly Lock _queueReadLock = new Lock();
    static readonly ConcurrentQueue<Log> _logQueue = new();
    static readonly TextWriter _consoleOutputStream = Console.Out;
    static readonly TextWriter _logFileStream;
    static BotLogger()
    {
        _logFileStream = new StreamWriter(File.Create(BotEnv.LogFile), Encoding.UTF8);
        _logWritebackLoopTask = Task.Factory.StartNew(WritebackLoop, TaskCreationOptions.LongRunning);
    }

    static void WritebackLoop()
    {
        var token = BotEnv.GlobalCanncellationToken;
        while (true)
        {
            Thread.Sleep(100);
            token.ThrowIfCancellationRequested();
            lock (_queueReadLock)
            {
                if (_logQueue.Count == 0)
                {
                    continue;
                }
                while (_logQueue.TryDequeue(out var log))
                {
                    var outputMsg = $"""
                    [{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}{(log.StackTrace is not null ? $"\n{log.StackTrace}" : string.Empty)}
                    """;
                    _consoleOutputStream.WriteLine(outputMsg);
                    _logFileStream.WriteLine(outputMsg);
                }
            }
        }
    }
    readonly struct Log
    {
        public DebugLevel Level { get; init; }
        public string Message { get; init; }
        public DateTime Timestamp { get; init; }
        public StackTrace? StackTrace { get; init; }
    }
    enum DebugLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
