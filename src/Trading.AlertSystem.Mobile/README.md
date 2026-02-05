# Trading Alert Mobile

.NET MAUI Android 移动应用，用于监控交易告警。

## 功能

- 📊 **监控状态** - 实时查看所有监控规则的状态
- 🔔 **告警规则** - 创建、编辑、删除价格告警规则
- 📜 **告警历史** - 查看历史告警记录
- 📈 **EMA配置** - 配置EMA穿越监控
- ⚙️ **设置** - 配置服务器地址和自动刷新

## 前提条件

1. **Visual Studio 2022** 或更高版本
2. 安装 **.NET MAUI 工作负载**
   - VS → 工具 → 获取工具和功能
   - 勾选 ".NET Multi-platform App UI 开发"
3. **Android SDK** (通过 VS 安装程序自动安装)
4. Android 模拟器或真机

## 开始使用

### 1. 启动后端服务

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

后端默认运行在 `http://localhost:5000`

### 2. 在 VS 中打开解决方案

双击 `TradingSystem.sln`

### 3. 设置启动项目

右键 `Trading.AlertSystem.Mobile` → 设为启动项目

### 4. 选择部署目标

工具栏选择：
- **Android Emulator** - 如果你有模拟器
- **真机** - 通过 USB 连接的 Android 手机

### 5. 运行

按 F5 或点击运行按钮

## 配置服务器地址

### 模拟器访问本机

模拟器中的 `localhost` 指向模拟器自身，要访问开发机器需要用特殊地址：

- **Android 模拟器**: `http://10.0.2.2:5000`

### 真机访问

1. 确保手机和电脑在同一网络
2. 获取电脑的局域网 IP（如 `192.168.1.100`）
3. 在应用设置中输入: `http://192.168.1.100:5000`

### 公网服务器

如果后端部署在公网，直接使用公网地址：
`https://your-server.com`

## 项目结构

```
Trading.AlertSystem.Mobile/
├── App.xaml                    # 应用入口
├── AppShell.xaml               # 导航结构
├── MauiProgram.cs              # DI 配置
├── Converters/
│   └── Converters.cs           # 数据绑定转换器
├── Models/
│   └── Models.cs               # 数据模型
├── Services/
│   ├── AlertApiClient.cs       # API 客户端
│   └── SettingsService.cs      # 设置存储
├── ViewModels/
│   ├── MonitorStatusViewModel.cs
│   ├── AlertListViewModel.cs
│   ├── AlertDetailViewModel.cs
│   ├── AlertHistoryViewModel.cs
│   ├── EmaConfigViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MonitorStatusPage.xaml
│   ├── AlertListPage.xaml
│   ├── AlertDetailPage.xaml
│   ├── AlertHistoryPage.xaml
│   ├── EmaConfigPage.xaml
│   └── SettingsPage.xaml
├── Resources/
│   ├── AppIcon/
│   ├── Fonts/
│   ├── Images/
│   ├── Splash/
│   └── Styles/
└── Platforms/
    └── Android/
```

## 添加字体（可选）

下载 OpenSans 字体并放到 `Resources/Fonts/` 目录：
- OpenSans-Regular.ttf
- OpenSans-Semibold.ttf

下载地址: https://fonts.google.com/specimen/Open+Sans

## 常见问题

### 1. 无法连接服务器

- 检查后端是否运行
- 检查服务器地址是否正确
- 检查防火墙设置
- 模拟器使用 `10.0.2.2` 而不是 `localhost`

### 2. 编译错误

- 确保安装了 MAUI 工作负载
- 尝试清理并重新生成解决方案
- 检查 Android SDK 是否正确安装

### 3. 模拟器启动慢

- 使用 x86_64 架构的模拟器镜像
- 启用 Hyper-V 或 HAXM 加速
- 考虑使用真机调试

## 生成 APK

### Debug APK

```bash
dotnet build -c Debug -f net8.0-android
```

APK 位置: `bin/Debug/net8.0-android/com.trading.alertsystem-Signed.apk`

### Release APK

```bash
dotnet publish -c Release -f net8.0-android
```

## 技术栈

- .NET 8.0
- .NET MAUI
- CommunityToolkit.Mvvm (MVVM 支持)
- System.Text.Json (JSON 序列化)
