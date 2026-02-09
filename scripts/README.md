# 测试和工具脚本

本目录包含项目的各类测试、验证和工具脚本。

## 脚本列表

### DeepSeek集成测试

#### test-deepseek.sh / test-deepseek.ps1
**用途**：测试DeepSeek API集成

**使用方法**：
```bash
# Linux/Mac
./scripts/test-deepseek.sh

# Windows PowerShell
.\scripts\test-deepseek.ps1
```

**测试内容**：
- DeepSeek API连接测试
- 基本对话功能验证
- 响应格式检查

#### test-deepseek-integration.ps1
**用途**：完整的DeepSeek集成验证

**使用方法**：
```powershell
.\scripts\test-deepseek-integration.ps1
```

**测试内容**：
- 双级AI服务配置验证
- DeepSeek客户端初始化
- 多提供商AI服务测试
- 成本优化模式验证

**输出示例**：
```
✅ DeepSeek客户端已初始化
✅ 多提供商双级AI服务已初始化 - Provider: DeepSeek
✅ 双级AI架构已启用 - 成本优化模式
```

### 项目验证脚本

#### verify-issue4-complete.sh
**用途**：验证Issue 4（基础设施重构）是否完成

**使用方法**：
```bash
./scripts/verify-issue4-complete.sh
```

**验证内容**：
- 检查Trading.Infras.*项目是否存在
- 验证项目引用关系
- 确认编译通过
- 检查配置文件完整性

### 开发工具

#### update-namespaces.ps1
**用途**：批量更新项目命名空间

**使用方法**：
```powershell
.\scripts\update-namespaces.ps1
```

**功能**：
- 自动查找和替换命名空间
- 更新using语句
- 保持代码格式

**注意事项**：
- 运行前请备份代码
- 建议在版本控制下运行
- 运行后检查git diff确认更改

## 运行环境

### Linux/Mac脚本 (.sh)
需要bash环境：
```bash
chmod +x scripts/*.sh
./scripts/script-name.sh
```

### Windows脚本 (.ps1)
需要PowerShell：
```powershell
# 如果遇到执行策略限制
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 运行脚本
.\scripts\script-name.ps1
```

## 最佳实践

1. **运行前备份**：重要更改前先提交git
2. **检查输出**：仔细阅读脚本输出信息
3. **验证结果**：运行后验证系统功能正常
4. **报告问题**：发现问题请在GitHub Issues中反馈

## 添加新脚本

如需添加新的测试脚本：

1. 将脚本文件放在此目录
2. 更新本README文档
3. 添加适当的注释和说明
4. 测试脚本在不同环境下的兼容性

## 相关文档

- [项目主文档](../README.md)
- [维护文档](../docs/maintenance/)
- [GitHub Issues](../GITHUB_ISSUES.md)
