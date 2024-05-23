# NekoBot

Hello! 这里是 NekoBot    ^ >ω< ^  
是一个使用 [Telegram.Bot](https://github.com/TelegramBots/telegram.bot) 进行开发的模块化 TelegramBot

## Usage

### 1. 获取可执行文件

您可以将repo clone到本地，自行构建

```bash
git clone https://github.com/LeZi9916/NekoBot.Core
cd NekoBot.Core
dotnet build
```

或直接从 [Release](https://github.com/LeZi9916/NekoBot/releases) 下载

### 2. 配置Bot

首次运行 NekoBot，Bot会在所在路径生成"Database"、"Temp"、"logs"、"Scripts"目录与"NekoBot.conf"配置文件  
请在"NekoBot.conf"中填入您的Bot信息

```yaml
Authenticator:        # HOTP相关
    Counter: 0
    Digits: 8         # HOTP Code长度
    SecretKey: ''     # 自动生成
    FailureCount: 0
DbAutoSave: true      # Database自动保存
AutoSaveInterval: 600 # Database在更改多久(秒)后保存
Token: ''             # 请输入您的Bot Token
Proxy:
    UseProxy: false   # 如为True,Bot将通过Proxy连接至Telegram
    Address: 
Analyzer:             # 统计器
    TotalHandleCount: 0
    TotalHandleTime: 0
Assembly:[]           # 引用的外部程序集
                      # e.g. ExampleAssembly.dll
```

## Assembly

NekoBot会在启动时会加载配置文件`Assembly`中定义的C#程序集，程序集需位于 `Scripts/Library`  
您可以在C# Script中使用 `using` 调用这些程序集的Namespace

## Extension

Extension是NekoBot的核心；NekoBot允许动态编译C# Script作为Extension，并在runtime动态地加载与卸载  
C# Script应当根据ExtensionType分类并存放在Scripts目录下

所有Extension必须实现接口`IExtension`

### Module

基类: `Extension`  
接口: `IExtension`  
目录: `Scripts/Module`

### Handler

基类: `Extension`  
接口: `IExtension`, `IHandler`  
目录: `Scripts/Handler`  
文件名: `{UpdateType}Handler.csx`  

处理botClient传入的`Update`并返回一个匿名函数交由`ScriptManager`调用

### Database

基类: `Database<T>`  
接口: `IExtension`, `IDatabase<T>`, `IDestroyable`  
目录: `Scripts/Database`  

### Serializer

基类: `Extension`  
接口: `IExtension`, `ISerializer`  
目录: `Scripts/Serializer`

## Enum

ExtensionType:

- Handler
- Module
- Database
- Serializer

UpdateType:

- Message
- InlineQuery
- ChosenInlineResult
- CallbackQuery
- EditedMessage
- ChannelPost
- EditedChannelPost
- ShippingQuery
- PreCheckoutQuery
- Poll
- PollAnswer
- MyChatMember
- ChatMember
- ChatJoinRequest
- Unknown
