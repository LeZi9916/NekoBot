using System.Threading;

namespace NekoBot.Types;

public class Destroyable : Extension
{
    public event System.Action? OnDestroy;
    protected CancellationTokenSource isDestroying = new();
    public virtual void Destroy()
    {
        if (OnDestroy is not null)
        {
            OnDestroy();
            OnDestroy = null;
        }
    }
}
