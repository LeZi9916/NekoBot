namespace NekoBot.Types;

public enum DebugType
{
    Debug,
    Info,
    Warning,
    Error
}
public enum ExtensionType
{
    Module,
    Database,
    Handler,
    Serializer
}
public enum Permission
{
    Unknown = -1,
    Ban,
    Common,
    Advanced,
    Admin,
    Root
}
public enum Action
{
    Ban,
    Reply,
    Delete
}
public enum Range
{
    Group,
    Global
}
enum CommandType
{
    Start,
    Add,
    Ban,
    Bind,
    Status,
    Help,
    Info,
    Promote,
    Demote,
    Mai,
    Logs,
    Config,
    Set,
    MaiStatus,
    MaiScanner,
    Unknow
}
