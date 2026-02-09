# 代码清理计划

**分支**: `chore/cleanup-codebase`
**日期**: 2026-02-09

---

## 📊 当前项目状态分析

### Git中追踪的项目 (11个)
- ✅ Trading.AI
- ✅ Trading.Core
- ✅ Trading.Data
- ✅ Trading.Infrastructure
- ✅ Trading.Services
- ✅ Trading.Infras.Web
- ✅ Trading.Strategy.Analyzer
- ⚠️ Trading.Backtest (4个项目)
  - Trading.Backtest
  - Trading.Backtest.Console
  - Trading.Backtest.Web
  - Trading.Backtest.ParameterOptimizer

### 未在Git中但存在于磁盘的文件夹 (4个)
- ❌ Trading.AlertSystem.Data (36MB - bin/obj)
- ❌ Trading.AlertSystem.Mobile (25MB - bin/obj)
- ❌ Trading.AlertSystem.Service (13MB - bin/obj)
- ❌ Trading.AlertSystem.Web (126MB - bin/obj + node_modules)

---

## 🗑️ 清理计划

### 1️⃣ 删除已从Git移除的文件夹 ⚠️ **高优先级**

这些文件夹已被重构为 `Trading.Infras.*`，但本地编译产物仍然存在：

```bash
# 删除 AlertSystem 遗留文件夹 (总计约 200MB)
rm -rf src/Trading.AlertSystem.Data
rm -rf src/Trading.AlertSystem.Mobile
rm -rf src/Trading.AlertSystem.Service
rm -rf src/Trading.AlertSystem.Web
```

**原因**: 这些项目已完成重构为 `Trading.Infras.*`，参考 Issue 4

---

### 2️⃣ 归档 Backtest 项目 ⚠️ **需要评估**

Backtest 项目占用空间巨大（特别是 ParameterOptimizer 5GB），建议归档：

**选项 A: 完全归档** (如果不再使用)
```bash
# 创建归档目录
mkdir -p archived/

# 移动整个 Backtest 相关项目
git mv src/Trading.Backtest archived/
git mv src/Trading.Backtest.Console archived/
git mv src/Trading.Backtest.Web archived/
git mv src/Trading.Backtest.ParameterOptimizer archived/

# 从解决方案中移除
# 需要编辑 TradingSystem.sln
```

**选项 B: 保留核心，归档工具** (推荐)
```bash
# 保留 Trading.Backtest (核心库，被 Strategy.Analyzer 引用)
# 归档其他工具项目
git mv src/Trading.Backtest.Console archived/
git mv src/Trading.Backtest.Web archived/
git mv src/Trading.Backtest.ParameterOptimizer archived/
```

**选项 C: 仅清理大文件，保留代码**
```bash
# 仅删除占用空间的 results/ 和 bin/obj/
rm -rf src/Trading.Backtest.ParameterOptimizer/results/
rm -rf src/Trading.Backtest.ParameterOptimizer/bin/
rm -rf src/Trading.Backtest.ParameterOptimizer/obj/
rm -rf src/Trading.Backtest.Web/wwwroot/node_modules/
rm -rf src/Trading.Backtest.Web/wwwroot/dist/
```

**项目引用分析**:
- `Trading.Backtest` (核心库) 被以下项目引用：
  - Trading.Strategy.Analyzer ✅ (使用中)
  - Trading.Backtest.Console
  - Trading.Backtest.Web
  - Trading.Backtest.ParameterOptimizer

⚠️ **建议**: 如果 Issue 9 (回测系统) 计划重新实现，选择 **选项 B**

---

### 3️⃣ 未被引用的文件检查 ✅ **已完成**

检查发现：**所有在 Git 中的项目都有被引用或是独立应用**

**项目引用树**:
```
Trading.Infras.Web (主应用)
├── Trading.Services
│   ├── Trading.AI
│   │   └── Trading.Core
│   ├── Trading.Infrastructure
│   │   └── Trading.Data
│   └── Trading.Core

Trading.Strategy.Analyzer (独立工具)
├── Trading.Backtest
│   ├── Trading.Core
│   └── Trading.Data
├── Trading.Core
└── Trading.Data

Trading.Backtest.Console (独立应用)
├── Trading.Backtest
└── Trading.Data

Trading.Backtest.Web (独立应用)
├── Trading.Backtest
└── Trading.Data

Trading.Backtest.ParameterOptimizer (独立应用)
├── Trading.Backtest
├── Trading.Core
└── Trading.Data
```

**结论**: 没有完全未被引用的项目需要删除

---

## 📝 建议的清理步骤

### 阶段 1: 删除已废弃的 AlertSystem 文件夹 ✅ **安全**

```bash
# 这些文件夹已不在 Git 中，可以安全删除
rm -rf src/Trading.AlertSystem.Data
rm -rf src/Trading.AlertSystem.Mobile
rm -rf src/Trading.AlertSystem.Service
rm -rf src/Trading.AlertSystem.Web

# 预计释放空间: 约 200MB
```

### 阶段 2: 清理 Backtest 大文件 ✅ **安全**

```bash
# 清理编译产物和临时文件（这些已在 .gitignore 中）
rm -rf src/Trading.Backtest/bin
rm -rf src/Trading.Backtest/obj
rm -rf src/Trading.Backtest.Console/bin
rm -rf src/Trading.Backtest.Console/obj
rm -rf src/Trading.Backtest.Console/reports
rm -rf src/Trading.Backtest.Console/cosmos_test_data
rm -rf src/Trading.Backtest.Web/bin
rm -rf src/Trading.Backtest.Web/obj
rm -rf src/Trading.Backtest.Web/wwwroot/dist
rm -rf src/Trading.Backtest.Web/wwwroot/node_modules
rm -rf src/Trading.Backtest.ParameterOptimizer/bin
rm -rf src/Trading.Backtest.ParameterOptimizer/obj
rm -rf src/Trading.Backtest.ParameterOptimizer/results

# 预计释放空间: 约 5-6GB
```

### 阶段 3: 归档 Backtest 工具项目 ⚠️ **需要确认**

**请确认以下问题后再执行**:
1. Issue 9 (回测系统) 是否计划重新实现？
2. 当前的 Backtest.Console/Web/ParameterOptimizer 是否还需要？
3. 是否有历史数据需要迁移？

**如果确认归档，执行**:
```bash
# 创建归档目录
mkdir -p archived

# 归档旧的回测工具 (保留核心库)
git mv src/Trading.Backtest.Console archived/
git mv src/Trading.Backtest.Web archived/
git mv src/Trading.Backtest.ParameterOptimizer archived/

# 更新 TradingSystem.sln (需要手动编辑)
# 移除以下项目引用:
# - Trading.Backtest.Console
# - Trading.Backtest.Web
# - Trading.Backtest.ParameterOptimizer
```

---

## 🎯 推荐执行方案

### 方案 A: 保守清理 (推荐先执行)

**仅删除已废弃的 AlertSystem 文件夹和编译产物**

```bash
# 1. 删除 AlertSystem 遗留文件夹
rm -rf src/Trading.AlertSystem.Data
rm -rf src/Trading.AlertSystem.Mobile
rm -rf src/Trading.AlertSystem.Service
rm -rf src/Trading.AlertSystem.Web

# 2. 清理 bin/obj (所有项目)
find src/ -type d -name "bin" -o -name "obj" | xargs rm -rf

# 3. 清理 Backtest 特定的大文件
rm -rf src/Trading.Backtest.Console/reports
rm -rf src/Trading.Backtest.Console/cosmos_test_data
rm -rf src/Trading.Backtest.Web/wwwroot/dist
rm -rf src/Trading.Backtest.Web/wwwroot/node_modules
rm -rf src/Trading.Backtest.ParameterOptimizer/results

# 预计释放空间: 6-7GB
# Git 改动: 无 (这些都在 .gitignore 中)
```

### 方案 B: 完全清理 (需要你确认)

**在方案 A 基础上，归档旧 Backtest 工具**

```bash
# 执行方案 A 的所有步骤，然后：

# 归档旧回测工具
mkdir -p archived
git mv src/Trading.Backtest.Console archived/
git mv src/Trading.Backtest.Web archived/
git mv src/Trading.Backtest.ParameterOptimizer archived/

# 需要手动编辑 TradingSystem.sln
# 移除归档项目的引用
```

---

## ❓ 需要你确认的问题

1. **Backtest 项目处理方式**:
   - [ ] 方案 A: 仅清理大文件，保留所有代码
   - [ ] 方案 B: 归档 Console/Web/ParameterOptimizer，保留核心库
   - [ ] 方案 C: 完全归档所有 Backtest 相关项目

2. **其他需要清理的**:
   - [ ] 是否有其他临时文件需要删除？
   - [ ] 是否需要清理 `data/` 目录下的 CSV 文件？

---

## 📋 执行清单

完成后请勾选：

- [ ] 删除 AlertSystem 文件夹
- [ ] 清理 bin/obj 编译产物
- [ ] 清理 Backtest 大文件
- [ ] (可选) 归档旧 Backtest 工具
- [ ] (可选) 更新 TradingSystem.sln
- [ ] 验证项目编译通过
- [ ] 提交清理改动

---

**请告知你选择的方案，我将执行相应的清理操作。**
