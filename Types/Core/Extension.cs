using System;
using System.Collections.Generic;
using System.Reflection;

namespace NekoBot.Types;
public class Extension
{
    protected Dictionary<Guid, CallbackHandler<CallbackMsg>> callbackTasks = new();
    public ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "Default",
        Version = new Version("1.0"),
        Type = ExtensionType.Module
    };
    public virtual void Handle(Message userMsg)
    {

    }
    public virtual void Init()
    {

    }
    public virtual void Save()
    {

    }
    public virtual MethodInfo? GetMethod(string methodName) => Info.ExtAssembly.GetType().GetMethod(methodName);
    public static void Debug(DebugType type, string message) => Core.Debug(type, message);
    public static string StringHandle<T>(T obj)
    {
        string s = obj?.ToString() ?? string.Empty;
        string[] reservedChar = { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        foreach (var c in reservedChar)
            s = s.Replace(c, $"\\{c}");
        return s;

    }
    public static string MakeCodeEntity(string? codeStr,string lang = "copy") => $"<pre><code class=\"{lang}\">{codeStr}</code></pre>";
    public static string MakeCodeEntity<T>(T obj,string lang = "copy") => MakeCodeEntity(obj?.ToString(),lang);
}
