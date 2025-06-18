using System;

namespace NekoBot.Chat;

public readonly struct ReadOnlyUser
{
    public long Id
    {
        get => _origin.Id;
    }
    public bool IsBot
    {
        get
        {
            return Id < 0;
        }
    }
    public string FirstName
    {
        get => _origin.FirstName;
    }
    public string? LastName
    {
        get => _origin.LastName;
    }
    public string? Username
    {
        get => _origin.LastName;
    }
    public string? LanguageCode
    {
        get => _origin.LanguageCode;
    }
    public bool IsPremium
    {
        get => _origin.IsPremium;
    }
    readonly NekoUser _origin;
    public ReadOnlyUser(NekoUser origin)
    {
        if (origin is null)
        {
            throw new ArgumentNullException(nameof(origin));
        }
        _origin = origin;
    }
}
