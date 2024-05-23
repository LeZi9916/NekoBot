using System;
using System.Linq;

namespace NekoBot.Types;

public struct Command
{
    public string Prefix;
    public string[] Params;
    public static Command? Parse(string s)
    {
        if (s.Length == 0 || s.Substring(0, 1) != "/") return null;

        var cmd = s.Split(new string[] { " ", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var prefix = cmd[0].Remove(0, 1);
        var param = cmd.Skip(1).ToArray();

        return new Command()
        {
            Prefix = prefix,
            Params = param
        };
    }
}
