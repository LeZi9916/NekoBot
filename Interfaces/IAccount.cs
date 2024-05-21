using NekoBot.Types;

namespace NekoBot.Interfaces;
internal interface IAccount
{
    long Id { get; set; }
    string? Username { get; set; }
    string? Name { get; }
    Permission Level { get; set; }
    void SetPermission(Permission targetLevel);
    bool CheckPermission(Permission targetLevel);
}
