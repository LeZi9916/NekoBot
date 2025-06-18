using System;
using Telegram.Bot.Types;

namespace NekoBot.Chat;

public class NekoUser
{
    public required long Id { get; init; }

    public bool IsBot
    {
        get
        {
            return Id < 0;
        }
    }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsPremium { get; set; }

    public override bool Equals(object? obj)
    {
        switch (obj)
        {
            case NekoUser a:
                return this == a;
            case User b:
                return this == b;
            default:
                return ReferenceEquals(this, obj);
        }
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(NekoUser a, NekoUser b)
    {
        return a.Id == b.Id;
    }
    public static bool operator !=(NekoUser a, NekoUser b)
    {
        return !(a == b);
    }
    public static bool operator ==(NekoUser a, User b)
    {
        return a.Id == b.Id;
    }
    public static bool operator !=(NekoUser a, User b)
    {
        return !(a == b);
    }
    public static implicit operator ReadOnlyUser(NekoUser u)
    {
        return new(u);
    }
}
