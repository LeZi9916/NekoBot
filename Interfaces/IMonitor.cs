namespace NekoBot.Interfaces;
public interface IMonitor<T> : IExtension
{
    T GetResult();
}
