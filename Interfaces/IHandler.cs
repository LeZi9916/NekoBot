using NekoBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Interfaces;

public interface IHandler: IExtension
{
    void Handle(ref Message userMsg);
}
