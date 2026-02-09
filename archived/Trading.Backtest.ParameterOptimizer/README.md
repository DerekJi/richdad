# Pin Bar 策略参数优化器

这是一个用于自动化测试和优化Pin Bar交易策略参数的工具，包含参数优化和结果分析两个功能。

## 📁 目录结构

```
Trading.Backtest.ParameterOptimizer/
├── Program.cs              # 主程序（包含optimizer和analyzer两个命令）
├── Models/                 # 数据模型
│   ├── BacktestParameters.cs
│   ├── OptimizationResult.cs
│   └── ParameterSpace.cs
├── Services/               # 核心服务
│   ├── BacktestExecutor.cs
│   ├── ResultsManager.cs
│   └── ParameterOptimizer.cs
├── Helpers/                # 辅助工具
│   └── ParameterRangeHelper.cs
├── results/               # 优化结果输出目录（gitignored）
│   ├── checkpoint_*.json  # 检查点文件
│   └── optimization_report_*.md  # 分析报告
├── analyze.bat            # Windows快捷分析脚本
├── analyze.sh             # Linux/Mac快捷分析脚本
└── README.md              # 本文件
```

## 🚀 快速开始

### 1. 运行参数优化

```bash
cd src/Trading.Backtest.ParameterOptimizer

# 方式1: 直接运行（默认命令）
dotnet run

# 方式2: 指定命令
dotnet run -- optimize
```

优化器会：
- 加载历史K线数据
- 测试所有参数组合
- 每500个测试保存一次检查点
- 实时显示进度和预计完成时间

### 2. 分析优化结果

**方式1: 使用快捷脚本（推荐）**

Windows:
```batch
analyze.bat
```

Linux/Mac:
```bash
./analyze.sh
```

**方式2: 使用命令行**

```bash
# 分析最新的checkpoint文件
dotnet run -- analyze

# 分析指定的文件
dotnet run -- analyze results/checkpoint_20260116_113522.json
```

分析工具会：
- 自动找到最新的checkpoint文件（如未指定）
- 提取收益率Top 10的参数组合
- 生成详细的Markdown分析报告
- 在控制台显示结果摘要

### 3. 查看分析报告

```bash
# 查看最新生成的报告
ls -lh results/optimization_report_*.md

# 在VS Code中打开
code results/optimization_report_<timestamp>.md
```

## 📊 报告内容

生成的分析报告包含：

### 🎯 核心发现
- **Top 10共同特征**: 识别出最优参数的共同模式
- **最佳参数配置**: 排名第1的完整参数设置
- **关键洞察**: 基于数据的策略优化建议

### 📈 详细数据
- Top 10完整排名和参数
- 参数分布统计（最小值、最大值、众数）
- 每组参数的收益率、胜率、交易数等指标

## 🔧 自定义参数空间

编辑 `Program.cs` 中的参数空间配置：

```csharp
var parameterSpace = new ParameterSpace
{
    MaxBodyPercentages = ParameterRangeHelper.SetRange(25, 30, 5),
    MinLongerWickPercentages = ParameterRangeHelper.SetRange(40, 60, 5),
    MaxShorterWickPercentages = ParameterRangeHelper.SetRange(25, 40, 5),
    NearEmaThresholds = ParameterRangeHelper.SetRange(0.8m, 2.3m, 0.3m),
    StopLossAtrRatios = ParameterRangeHelper.SetRange(1.0m, 1.5m, 0.5m),
    RiskRewardRatios = ParameterRangeHelper.SetRange(1.5m, 2.5m, 0.5m),
    MaxLossPerTradePercents = ParameterRangeHelper.SetRange(0.5m, 1.0m, 0.1m)
};
```

使用 `ParameterRangeHelper.SetRange()` 方法轻松定义范围：
- `SetRange(start, end, step)` - 生成等差数列

## 📝 优化建议

基于当前的分析结果：

1. **风险回报比**: 使用 **2.5** 而不是1.5-2.0
2. **单笔最大亏损**: 允许 **1.0%** 可以显著提升收益
3. **Pin Bar形状**: 
   - 实体占比上限: 25-30%
   - 长影线占比下限: 40-60%
   - 短影线占比上限: 25-40%
4. **胜率预期**: 不追求高胜率（30-36%即可），关键是高盈亏比

## ⚙️ 性能优化

- **预加载数据**: 一次性加载CSV数据，避免重复IO
- **内存优化**: 直接使用已加载的Candle集合
- **检查点机制**: 每500个测试自动保存，防止意外中断
- **并行处理**: 可以通过修改代码启用多线程（需要注意线程安全）

## 🛠️ 故障排除

### 优化器运行错误

**问题**: "DivideByZeroException"
**解决**: 确保 `ContractSize` 已在配置中设置

### 找不到checkpoint文件

**问题**: `analyze.bat` 提示找不到文件
**解决**: 先运行 `dotnet run` 生成至少一个checkpoint

### 内存不足

**问题**: 程序崩溃或OOM
**解决**: 减小参数空间范围，或增加step步长

## 📚 相关文档

- [Pin Bar策略说明](../../docs/pin-bar.strategy.md)
- [SOLID架构设计](./Models/README.md)
- [API文档](../../docs/api.md)

## 📄 许可证

MIT License
