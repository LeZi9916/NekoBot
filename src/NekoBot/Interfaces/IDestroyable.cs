using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Interfaces;
public interface IDestroyable: IExtension
{
    event Action? OnDestroy;
    void Destroy();
}
