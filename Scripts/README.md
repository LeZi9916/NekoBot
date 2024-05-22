# NekoBot

---
Hello! 这里是 NekoBot    ^ >ω< ^
是一个使用 [Telegram.Bot](https://github.com/TelegramBots/telegram.bot) 进行开发的模块化 TelegramBot

## Usage

首次运行 NekoBot，Bot会在所在路径生成"Database"、"Temp"、"Scripts"目录与"NekoBot.conf"配置文件
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
    UseProxy: false   # Bot是否通过Proxy连接至Telegram
    Address: 
Analyzer:             # 统计器
    TotalHandleCount: 0
    TotalHandleTime: 0
```

## Extension

Extension是NekoBot的核心；NekoBot允许动态编译C# Script作为Extension，并在runtime动态地加载与卸载
C# Script应当根据ExtensionType分类并存放在Scripts目录下

### Type

NekoBot内建了一些Extension Type:

- Handler
- Module
- Database
- Serializer
