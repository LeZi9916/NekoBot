using NekoBot.Types;
using System.Reflection;
using Message = NekoBot.Types.Message;

#nullable enable
namespace NekoBot.Interfaces
{
    public interface IExtension
    {
        /// <summary>
        /// Extension Info
        /// <para>include name,version,commands,dependencies and assembly</para>
        /// </summary>
        public ExtensionInfo Info { get; }
        void Handle(Message msg);
        void Init();
        void Save();
        void Destroy();
        MethodInfo? GetMethod(string methodName);
    }
}
