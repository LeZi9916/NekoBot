using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Interfaces;
public interface ICallbackHandler
{
    void AddCallbackFunc(in CallbackHandler<CallbackMsg> func);
}
