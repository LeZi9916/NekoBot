using System;

namespace NekoBot.Exceptions;
public class DependNotFoundException: Exception
{
    public DependNotFoundException(string s) : base(s) { }
}
