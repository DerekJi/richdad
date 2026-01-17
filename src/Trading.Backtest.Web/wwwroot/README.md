# 前端开发指南

## 技术栈

- **TypeScript 5.9.3**: 类型安全的JavaScript超集
- **Vite 7.3.1**: 快速的前端构建工具
- **Chart.js 4.4+**: 图表库
- **原生ES6+**: 无其他框架依赖，保持轻量

## 项目结构

```
wwwroot/
├── src/                        # TypeScript源代码
│   ├── components/             # UI组件
│   │   ├── StrategyManager.ts  # 策略管理组件
│   │   ├── BacktestRunner.ts   # 回测执行组件
│   │   ├── ChartManager.ts     # 图表管理组件
│   │   └── TradeTable.ts       # 交易表格组件
│   ├── services/               # 服务层
│   │   └── backtestService.ts  # API调用服务
│   ├── types/                  # TypeScript类型定义
│   │   └── api.ts              # API接口类型
│   ├── utils/                  # 工具函数
│   │   └── helpers.ts          # 通用辅助函数
│   └── main.ts                 # 应用入口
├── css/                        # 样式文件
├── js/                         # 旧的JavaScript文件(已弃用)
├── dist/                       # 构建输出目录(gitignored)
├── index.html                  # 主页
├── pinbar-backtest.html        # 回测页面
├── package.json                # npm配置
├── tsconfig.json               # TypeScript配置
└── vite.config.ts              # Vite配置
```

## 开发工作流

### 1. 安装依赖

首次使用或依赖更新后执行：

```bash
cd src/Trading.Backtest.Web/wwwroot
npm install
```

### 2. 开发模式

启动开发服务器，支持热更新：

```bash
npm run dev
```

- 前端服务器: http://localhost:5173（开发模式）
- 生产服务器: http://localhost:5243
- API: /api（使用相对路径，自动适配当前端口）
- 修改代码后自动刷新

**注意**:
- 开发模式(npm run dev)使用端口5173
- 生产模式(dotnet run)直接访问5243端口，前端已编译到wwwroot/dist

### 3. 生产构建

构建优化后的生产版本：

```bash
npm run build
```

- 输出目录: `dist/`
- 包含类型检查 + 代码打包
- 自动压缩和优化

### 4. 预览生产版本

```bash
npm run preview
```

本地预览构建后的生产版本

## 代码规范

### TypeScript类型

所有API接口类型都定义在 `src/types/api.ts`:

```typescript
import type { BacktestRequest, BacktestResponse } from '../types/api';
```

### 组件模式

每个组件都导出一个单例实例:

```typescript
export class StrategyManager {
  // ...
}

export const strategyManager = new StrategyManager();
```

### 服务层

API调用通过服务层统一管理:

```typescript
import { backtestService } from '../services/backtestService';

const response = await backtestService.runBacktest(request);
```

## Vite配置说明

### 多页面应用

配置在 `vite.config.ts`:

```typescript
input: {
  main: resolve(__dirname, 'index.html'),
  backtest: resolve(__dirname, 'pinbar-backtest.html')
}
```

### API代理

开发模式下自动代理API请求:

```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5002',
    changeOrigin: true
  }
}
```

## 常见问题

### Q: 修改代码后浏览器没有更新？

A: Vite支持HMR(热模块替换)，应该自动更新。如果没有:
1. 检查浏览器控制台错误
2. 尝试手动刷新页面
3. 重启dev服务器

### Q: API请求失败？

A: 确保:
1. 后端服务器运行在 http://localhost:5002
2. API代理配置正确
3. 检查浏览器Network面板

### Q: TypeScript类型错误？

A:
1. 运行 `npm run build` 查看详细错误
2. 确保类型定义与后端API一致
3. 使用 `// @ts-ignore` 临时跳过(不推荐)

### Q: 如何添加新组件？

A:
1. 在 `src/components/` 创建 `.ts` 文件
2. 定义类并导出单例
3. 在 `src/main.ts` 导入并初始化
4. 更新类型定义(如需要)

## 性能优化

- ✅ 代码分割: Vite自动处理
- ✅ Tree-shaking: 移除未使用代码
- ✅ 资源压缩: 生产构建自动压缩
- ✅ 懒加载: Chart.js等库按需加载

## 调试技巧

### 浏览器DevTools

- **源代码映射**: 支持在原始TypeScript中断点调试
- **Network**: 检查API请求和响应
- **Console**: 查看日志和错误

### VSCode调试

1. 安装 "JavaScript Debugger" 扩展
2. 在TypeScript代码中打断点
3. F5启动调试

## 下一步优化(可选)

- [ ] 添加单元测试(Vitest)
- [ ] 集成ESLint + Prettier
- [ ] 添加状态管理(如需要)
- [ ] 使用Alpine.js/Petite-Vue增强交互(如需要)
- [ ] PWA支持
- [ ] 国际化(i18n)

## 参考文档

- [TypeScript手册](https://www.typescriptlang.org/docs/)
- [Vite文档](https://vitejs.dev/)
- [Chart.js文档](https://www.chartjs.org/)
