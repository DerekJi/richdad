# Phase 2 验证指南

本指南说明如何验证 Phase 2 四级决策模型的功能。

## 快速验证

### 方法 1: 使用验证脚本（推荐）

```powershell
# 在项目根目录运行
.\scripts\verify-phase2.ps1
```

脚本会自动：
1. ✅ 检查服务器状态
2. ✅ 必要时启动服务器
3. ✅ 运行所有验证测试
4. ✅ 显示测试结果

### 方法 2: 手动验证

#### 步骤 1: 启动服务器

```bash
cd src/Trading.Web
dotnet run
```

等待服务器启动完成（显示 "Now listening on: http://localhost:5000"）

#### 步骤 2: 运行测试

**使用 curl:**
```bash
# 运行所有测试
curl http://localhost:5000/api/phase2validation/all

# 测试 JSON 序列化
curl http://localhost:5000/api/phase2validation/json

# 测试级联验证
curl http://localhost:5000/api/phase2validation/context

# 测试便捷属性
curl http://localhost:5000/api/phase2validation/properties
```

**使用 PowerShell:**
```powershell
# 运行所有测试（格式化输出）
Invoke-RestMethod -Uri "http://localhost:5000/api/phase2validation/all" | ConvertTo-Json -Depth 10
```

**使用浏览器:**
- 访问 http://localhost:5000/api/phase2validation/all

## 验证内容

### 1. JSON 序列化测试
验证所有模型类的 JSON 序列化和反序列化功能：
- ✅ DailyBias
- ✅ StructureAnalysis
- ✅ SignalDetection
- ✅ FinalDecision

### 2. TradingContext 级联验证
验证四级级联验证逻辑，包括：
- ✅ 完整通过场景（所有层级有效）
- ✅ L1 失败场景（置信度不足）
- ✅ L2 失败场景（状态为 Idle）
- ✅ L3 失败场景（无信号）

### 3. 便捷属性测试
验证所有便捷属性和方法：
- ✅ DailyBias 属性（IsStrongBullish, IsConfident 等）
- ✅ StructureAnalysis 属性（CanTrade, IsTrending 等）
- ✅ SignalDetection 属性（HasSignal, RiskRewardRatio 等）
- ✅ FinalDecision 属性（ShouldExecute, TotalRiskAmount 等）

## 预期结果

成功的验证应该显示：

```json
{
  "success": true,
  "message": "✅ Phase 2 所有验证测试通过！",
  "timestamp": "2026-02-10T03:13:25.881875Z",
  "results": {
    "jsonSerialization": { "success": true },
    "contextValidation": { "success": true },
    "convenienceProperties": { "success": true }
  }
}
```

## 故障排除

### 服务器无法启动
```bash
# 检查端口是否被占用
netstat -ano | findstr :5000

# 编译项目
dotnet build src/Trading.Web
```

### 测试失败
```bash
# 查看服务器日志
cd src/Trading.Web
dotnet run
# 注意观察控制台输出的错误信息
```

### 编译错误
```bash
# 清理并重新编译
dotnet clean
dotnet build
```

## 验证文档

- **实现报告**: [PHASE2_COMPLETION_REPORT.md](../docs/issues/planned/PHASE2_COMPLETION_REPORT.md)
- **验证报告**: [PHASE2_VALIDATION_REPORT.md](../docs/issues/planned/PHASE2_VALIDATION_REPORT.md)

## 相关文件

- **控制器**: `src/Trading.Web/Controllers/Phase2ValidationController.cs`
- **模型类**: `src/Trading.Models/Models/*.cs`
- **验证脚本**: `scripts/verify-phase2.ps1`

---

**最后更新**: 2026-02-10
**状态**: ✅ 验证通过
