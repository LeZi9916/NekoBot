using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NekoBot;

public static class BotLogger
{

    static Task _logWritebackLoopTask = Task.CompletedTask;

    static readonly string _logFilename = Path.Combine(BotEnv.LogPath, $"{BotEnv.Up.ToString("yyyy-MM-dd HH-mm-ss")}.log");
    static readonly Lock _queueReadLock = new Lock();
    static readonly ConcurrentQueue<Log> _logQueue = new();
    static readonly TextWriter _consoleOutputStream = Console.Out;
    static readonly TextWriter _logFileStream;

    const string LOG_HEAD_FORMAT = "[0]";
    static BotLogger()
    {
        _logFileStream = new StreamWriter(File.Create(_logFilename), Encoding.UTF8);
        _logWritebackLoopTask = Task.Factory.StartNew(WritebackLoop, TaskCreationOptions.LongRunning);
        BotEnv.OnProcessExit += OnProcessExit;
    }
    public static void Debug(string msg)
    {
        _logQueue.Enqueue(new()
        {
            Level = DebugLevel.Debug,
            Message = msg,
            Timestamp = DateTime.Now,
            StackTrace = GetStackTrace()
        });
    }
    public static void Info(string msg)
    {
        _logQueue.Enqueue(new()
        {
            Level = DebugLevel.Info,
            Message = msg,
            Timestamp = DateTime.Now,
            StackTrace = GetStackTrace()
        });
    }
    public static void Warning(string msg)
    {
        _logQueue.Enqueue(new()
        {
            Level = DebugLevel.Warning,
            Message = msg,
            Timestamp = DateTime.Now,
            StackTrace = GetStackTrace()
        });
    }
    public static void Error(string msg)
    {
        _logQueue.Enqueue(new()
        {
            Level = DebugLevel.Error,
            Message = msg,
            Timestamp = DateTime.Now,
            StackTrace = GetStackTrace()
        });
    }
    [DoesNotReturn]
    public static void Fatal(string msg, int exitCode = -127)
    {
        _logQueue.Enqueue(new()
        {
            Level = DebugLevel.Fatal,
            Message = msg,
            Timestamp = DateTime.Now,
            StackTrace = GetStackTrace()
        });
        Environment.Exit(exitCode);
    }
    static StackTrace? GetStackTrace()
    {
#if DEBUG
        return new StackTrace(2);
#else
        return null;
#endif
    }
    static void WritebackLoop()
    {
        var sb = new StringBuilder(128);
        var token = BotEnv.GlobalCanncellationToken;
        var charArrayPool = ArrayPool<char>.Shared;
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
                    sb.Append(string.Format(LOG_HEAD_FORMAT, log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                    sb.Append(' ');
                    sb.Append(string.Format(LOG_HEAD_FORMAT, log.Level));
                    sb.Append(' ');
                    sb.Append(log.Message);
                    if(log.StackTrace is not null)
                    {
                        sb.AppendLine("StackTrace:");
                        sb.AppendLine(log.StackTrace.ToString());
                    }
                    var length = sb.Length;
                    var originArray = charArrayPool.Rent(length);
                    var charBuffer = originArray.AsSpan().Slice(0, length);

                    try
                    {
                        sb.CopyTo(0, originArray, length);
                        _consoleOutputStream.WriteLine(charBuffer);
                        _logFileStream.WriteLine(charBuffer);
                        sb.Clear();
                    }
                    finally
                    {
                        charArrayPool.Return(originArray);
                    }
                }
            }
        }
    }
    static void OnProcessExit(object? sender, EventArgs e)
    {
        var sb = new StringBuilder(128);
        lock (_queueReadLock)
        {
            if (_logQueue.Count == 0)
            {
                return;
            }
            while (_logQueue.TryDequeue(out var log))
            {
                sb.Append(string.Format(LOG_HEAD_FORMAT, log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
                sb.Append(' ');
                sb.Append(string.Format(LOG_HEAD_FORMAT, log.Level));
                sb.Append(' ');
                sb.Append(log.Message);
                if (log.StackTrace is not null)
                {
                    sb.AppendLine("StackTrace:");
                    sb.AppendLine(log.StackTrace.ToString());
                }
                var length = sb.Length;
                var originArray = new char[length];
                var charBuffer = originArray.AsSpan().Slice(0, length);

                sb.CopyTo(0, originArray, length);
                _logFileStream.WriteLine(charBuffer);
                sb.Clear();
            }
        }
        _logFileStream.Close();
    }
    readonly struct Log
    {
        public required DebugLevel Level { get; init; }
        public required string Message { get; init; }
        public required DateTime Timestamp { get; init; }
        public StackTrace? StackTrace { get; init; }
    }
    enum DebugLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }
}
