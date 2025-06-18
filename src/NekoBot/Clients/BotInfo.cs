using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Clients;
internal readonly struct BotInfo
{
    public required long Id { get; init; }
    public string Name
    {
        get
        {
            if(string.IsNullOrEmpty(_lastName))
            {
                return _firstName;
            }
            return _name;
        }
    }
    public required string FirstName 
    {
        get => _firstName;
        init
        {
            _firstName = value;
            _name = $"{_firstName} {_lastName}";
        }
    }
    public string LastName
    {
        get => _lastName;
        init
        {
            _lastName = value;
            _name = $"{_firstName} {_lastName}";
        }
    }

    readonly string _name = string.Empty;
    readonly string _firstName = string.Empty;
    readonly string _lastName = string.Empty;

    public BotInfo()
    {

    }
}
