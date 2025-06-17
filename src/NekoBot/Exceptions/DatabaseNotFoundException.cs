using System;

namespace NekoBot.Exceptions;

public class DatabaseNotFoundException : Exception
{
    public DatabaseNotFoundException(string s) : base(s) { }
}
