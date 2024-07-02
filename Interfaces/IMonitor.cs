using System.Dynamic;

namespace NekoBot.Interfaces;
public interface IMonitor : IExtension
{
    ExpandoObject GetReport();
}
