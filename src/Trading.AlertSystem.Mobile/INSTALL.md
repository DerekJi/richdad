# Trading Alert Mobile - 完整安装指南

MAUI 移动端项目已创建完成，但需要安装 MAUI workload 才能构建。

## 选项 1: 使用 Visual Studio 2022（推荐）

### 1. 安装 MAUI 工作负载

打开 **Visual Studio Installer**：
- 点击"修改"
- 勾选 ".NET Multi-platform App UI 开发"
- 点击"修改"安装

安装完成后 VS 会自动配置好：
- ✅ Android SDK
- ✅ Android Emulator
- ✅ Java JDK
- ✅ 所有依赖项

### 2. 重新添加项目到解决方案

```bash
cd D:/source/richdad
dotnet sln add src/Trading.AlertSystem.Mobile/Trading.AlertSystem.Mobile.csproj
```

### 3. 在 VS 中打开并运行

1. 双击 `TradingSystem.sln`
2. 右键 `Trading.AlertSystem.Mobile` → 设为启动项目
3. 工具栏选择 Android Emulator
4. F5 运行

---

## 选项 2: 命令行安装（需要管理员权限）

### 1. 以管理员身份运行 PowerShell 或 CMD

```powershell
# 安装 MAUI Android workload
dotnet workload install maui-android

# 如果失败，先清理缓存
dotnet workload clean
dotnet nuget locals all --clear
dotnet workload install maui-android
```

### 2. 安装 Android SDK（如果没有 VS）

需要手动下载并配置：
- Android SDK
- Android SDK Platform-Tools
- Android SDK Build-Tools
- Android Emulator

下载地址: https://developer.android.com/studio/command-line

### 3. 重新添加到解决方案

```bash
cd D:/source/richdad
dotnet sln add src/Trading.AlertSystem.Mobile/Trading.AlertSystem.Mobile.csproj
```

### 4. 构建和运行

```bash
cd D:/source/richdad/src/Trading.AlertSystem.Mobile

# 构建
dotnet build -f net9.0-android

# 运行（需要启动模拟器或连接真机）
dotnet build -t:Run -f net9.0-android
```

---

## 当前状态

✅ **已完成**：
- MAUI 项目结构已创建
- 所有代码文件已生成
- 所有其他项目可正常构建

⏳ **待完成**：
- 安装 MAUI workload（通过 VS 或命令行）

---

## 项目文件位置

```
d:/source/richdad/src/Trading.AlertSystem.Mobile/
├── README.md                 # 详细使用说明
├── Trading.AlertSystem.Mobile.csproj
├── App.xaml / App.xaml.cs
├── AppShell.xaml
├── MauiProgram.cs
├── Models/
├── Services/
├── ViewModels/
├── Views/
└── Resources/
```

---

## 功能完整列表

- 📊 监控状态 - 实时查看价格和 EMA 监控
- 🔔 告警规则管理 - 增删改查
- 📜 告警历史 - 分页查看
- 📈 EMA 配置 - 配置多品种多周期
- ⚙️ 设置 - 服务器地址、自动刷新

---

## 推荐方式

**强烈建议使用 Visual Studio 2022 安装 MAUI**，因为：
- 自动安装所有依赖
- 内置模拟器管理
- 更好的调试体验
- 一键运行
- 不需要管理员权限问题

命令行方式适合有经验的开发者或 CI/CD 环境。
