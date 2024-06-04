using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Interfaces;
public interface ICallbackHandler
{
    void AddCallbackFunc(IExtension submiter,Action<CallbackMsg> func);
    void RemoveAllFunc(IExtension submiter);
    void RemoveCallbackFunc(IExtension submiter, Action<CallbackMsg> func);
}
