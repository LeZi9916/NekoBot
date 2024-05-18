using Microsoft.CodeAnalysis.CSharp.Syntax;
using NekoBot.Interfaces;
using System.Reflection;

#nullable enable
namespace NekoBot.Types
{
    public class ExtensionCore : IExtension
    {
        public ExtensionInfo Info { get; } = new ExtensionInfo()
        {
            Name = "Default",
            Version = new Version() { Major = 1, Minor = 0 },
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
        public virtual void Destroy()
        {

        }
        public virtual MethodInfo? GetMethod(string methodName) => Info.ExtAssembly.GetType().GetMethod(methodName);
        public static void Debug(DebugType type, string message) => Core.Debug(type, message);    }
}
