# Cloudflare OpenNext 迁移实施计划

> **给 agentic workers:** REQUIRED SUB-SKILL: 使用 superpowers:executing-plans 按任务逐项执行。步骤使用 checkbox 语法跟踪。

**目标:** 将 `notion.sync.api.client` 从 Vercel 部署适配迁移到 Cloudflare 上可运行的 Next.js 全栈部署，同时尽量复现现有 SSR、ISR、Route Handler、动态 sitemap、Supabase 和 Notion 渲染功能。

**架构:** 当前项目使用 Next.js 15 App Router，存在 `revalidate`、`revalidatePath`、Route Handler、动态 sitemap 和 Vercel 后台任务。Cloudflare 官方文档说明，带 SSR 和 API 的 Next.js 应使用 Workers + OpenNext adapter；Cloudflare Pages 的 Next.js 指南主要适用于 static export，不适合当前功能集。因此代码层面按 OpenNext Cloudflare 适配进行迁移，部署入口由 Wrangler 指向 `.open-next/worker.js`，静态资源由 Workers Assets 提供。

**技术栈:** Next.js 15、React 19、TypeScript、pnpm、Supabase JS、OpenNext Cloudflare adapter、Wrangler、Cloudflare Workers Static Assets。

---

## 范围和约束

- 只修改 `notion.sync.api.client` 目录下的项目文件。
- 本计划文件也必须保存在 `notion.sync.api.client` 目录下，不在仓库根目录新增迁移文档。
- 不迁移后端 `.NET`、`notion.sync.api.node`、`AWS` 目录。
- 不把现有 SSR/API 功能降级为纯静态导出。
- 不修改现有中文文案和乱码样式文本。
- 不重构页面、组件、Supabase 查询、Notion 渲染逻辑。
- 不新增 Cloudflare KV、D1、Queues、R2 业务绑定，除非后续实际验证证明 ISR 缓存必须配置 R2。

## Cloudflare 文档依据

- Cloudflare Pages 的 Next.js 文档指出，全栈 SSR Next.js 应参考 Next.js Workers guide；Pages 的 Next.js static guide 只适合 static export。
- Cloudflare Workers Next.js 文档说明 `@opennextjs/cloudflare` 支持 App Router、Route Handlers、React Server Components、SSG、SSR、ISR、Server Actions、streaming、Middleware。
- OpenNext Cloudflare 需要 `@opennextjs/cloudflare`、`wrangler`、`open-next.config.ts`、`wrangler.jsonc`，并要求 `nodejs_compat` 和 `compatibility_date >= 2024-09-23`。
- Cloudflare Workers 支持 Next.js 的 `after` 异步任务能力，可替换当前 `@vercel/functions` 的后台预热逻辑，同时避免普通 `next build` 解析 `cloudflare:workers` 失败。

## 文件结构计划

- 修改 `notion.sync.api.client/package.json`
  - 新增 OpenNext 和 Wrangler 相关脚本。
  - 移除 Vercel 专用依赖。
  - 新增 Cloudflare 运行所需依赖。
- 修改 `notion.sync.api.client/pnpm-lock.yaml`
  - 由 `pnpm install` 更新。
- 新增 `notion.sync.api.client/wrangler.jsonc`
  - 配置 Worker 入口、静态资源目录、兼容日期、`nodejs_compat`、observability。
- 新增 `notion.sync.api.client/open-next.config.ts`
  - 使用 `defineCloudflareConfig()`。
- 修改 `notion.sync.api.client/next.config.ts`
  - 调用 OpenNext Cloudflare dev 初始化，让 `next dev` 保持现有体验。
- 修改 `notion.sync.api.client/src/app/api/cache-orchestrator/route.ts`
  - 将 `@vercel/functions` 的 `waitUntil` 替换为 Next.js `after`。
- 修改 `notion.sync.api.client/src/app/layout.tsx`
  - 移除 `@vercel/speed-insights/next` 导入和组件。
- 删除 `notion.sync.api.client/vercel.json`
  - Vercel 构建配置迁移后不再使用。

## 任务 1: 确认分支和安装 Cloudflare 依赖

**文件:**
- 修改: `notion.sync.api.client/package.json`
- 修改: `notion.sync.api.client/pnpm-lock.yaml`

- [ ] **步骤 1: 确认当前分支**

运行:

```powershell
git branch --show-current
```

期望:

```text
notion.sync.api.client
```

如果不是该分支，先切换到目标分支，再继续执行计划。不要在其它分支执行代码改动。

- [ ] **步骤 2: 安装 Cloudflare 适配依赖**

运行:

```powershell
pnpm --dir notion.sync.api.client add @opennextjs/cloudflare@latest
pnpm --dir notion.sync.api.client add -D wrangler@latest
```

期望:

```text
dependencies:
+ @opennextjs/cloudflare
devDependencies:
+ wrangler
```

- [ ] **步骤 3: 移除 Vercel 专用依赖**

运行:

```powershell
pnpm --dir notion.sync.api.client remove @vercel/functions @vercel/speed-insights
```

期望:

```text
dependencies:
- @vercel/functions
- @vercel/speed-insights
```

- [ ] **步骤 4: 提交依赖变更**

运行:

```powershell
git add notion.sync.api.client/package.json notion.sync.api.client/pnpm-lock.yaml
git commit -m "chore: add cloudflare next adapter"
```

## 任务 2: 添加 OpenNext 和 Wrangler 配置

**文件:**
- 新增: `notion.sync.api.client/open-next.config.ts`
- 新增: `notion.sync.api.client/wrangler.jsonc`
- 修改: `notion.sync.api.client/package.json`

- [ ] **步骤 1: 新增 OpenNext 配置**

创建 `notion.sync.api.client/open-next.config.ts`:

```ts
import { defineCloudflareConfig } from "@opennextjs/cloudflare";

export default defineCloudflareConfig();
```

- [ ] **步骤 2: 新增 Wrangler 配置**

创建 `notion.sync.api.client/wrangler.jsonc`:

```jsonc
{
  "$schema": "./node_modules/wrangler/config-schema.json",
  "name": "notion-sync-api-client",
  "main": ".open-next/worker.js",
  "compatibility_date": "2026-04-15",
  "compatibility_flags": ["nodejs_compat"],
  "assets": {
    "directory": ".open-next/assets",
    "binding": "ASSETS"
  },
  "observability": {
    "enabled": true
  }
}
```

- [ ] **步骤 3: 更新脚本**

修改 `notion.sync.api.client/package.json` 的 `scripts`:

```json
{
  "dev": "next dev --turbopack",
  "build": "next build --turbopack",
  "start": "next start",
  "lint": "eslint",
  "lint:fix": "eslint --fix",
  "postbuild": "next-sitemap",
  "cf:build": "opennextjs-cloudflare build",
  "cf:preview": "opennextjs-cloudflare build && opennextjs-cloudflare preview",
  "cf:deploy": "opennextjs-cloudflare build && opennextjs-cloudflare deploy",
  "cf:typegen": "wrangler types --env-interface CloudflareEnv cloudflare-env.d.ts"
}
```

- [ ] **步骤 4: 验证配置文件可被 TypeScript 解析**

运行:

```powershell
pnpm --dir notion.sync.api.client exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 5: 提交配置变更**

运行:

```powershell
git add notion.sync.api.client/open-next.config.ts notion.sync.api.client/wrangler.jsonc notion.sync.api.client/package.json
git commit -m "chore: configure cloudflare opennext"
```

## 任务 3: 迁移 Vercel 运行时代码

**文件:**
- 修改: `notion.sync.api.client/src/app/api/cache-orchestrator/route.ts`
- 修改: `notion.sync.api.client/src/app/layout.tsx`
- 删除: `notion.sync.api.client/vercel.json`

- [ ] **步骤 1: 替换后台任务 API**

修改 `notion.sync.api.client/src/app/api/cache-orchestrator/route.ts`:

```ts
import { revalidatePath } from "next/cache";
import { after, NextRequest, NextResponse } from "next/server";
import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
```

保留下面这行逻辑不变:

```ts
after(batchWarmup);
```

- [ ] **步骤 2: 移除 Vercel Speed Insights**

修改 `notion.sync.api.client/src/app/layout.tsx`:

删除导入:

```ts
import { SpeedInsights } from "@vercel/speed-insights/next";
```

删除组件:

```tsx
<SpeedInsights />
```

- [ ] **步骤 3: 删除 Vercel 配置**

删除:

```text
notion.sync.api.client/vercel.json
```

- [ ] **步骤 4: 验证不再引用 Vercel 包**

运行:

```powershell
rg "@vercel|vercel" notion.sync.api.client
```

期望:

```text
无输出
```

- [ ] **步骤 5: 验证 TypeScript**

运行:

```powershell
pnpm --dir notion.sync.api.client exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 6: 提交运行时代码变更**

运行:

```powershell
git add notion.sync.api.client/src/app/api/cache-orchestrator/route.ts notion.sync.api.client/src/app/layout.tsx notion.sync.api.client/vercel.json
git commit -m "chore: remove vercel runtime hooks"
```

## 任务 4: 接入 Cloudflare 开发类型

**文件:**
- 修改: `notion.sync.api.client/next.config.ts`
- 新增: `notion.sync.api.client/cloudflare-env.d.ts`
- 修改: `notion.sync.api.client/tsconfig.json`

- [ ] **步骤 1: 初始化 Cloudflare dev 环境**

修改 `notion.sync.api.client/next.config.ts`:

```ts
import { initOpenNextCloudflareForDev } from "@opennextjs/cloudflare";
import type { NextConfig } from "next";

initOpenNextCloudflareForDev();

const nextConfig: NextConfig = {
  transpilePackages: [
    "react-notion-x",
    "notion-types",
    "notion-utils",
    "lucide-react",
  ],
};

export default nextConfig;
```

- [ ] **步骤 2: 生成 Cloudflare 类型**

运行:

```powershell
pnpm --dir notion.sync.api.client run cf:typegen
```

期望:

```text
cloudflare-env.d.ts 已生成
```

- [ ] **步骤 3: 确认 TypeScript include 覆盖生成文件**

如果 `cloudflare-env.d.ts` 没有被当前 `include` 覆盖，修改 `notion.sync.api.client/tsconfig.json`:

```json
"include": [
  "next-env.d.ts",
  "cloudflare-env.d.ts",
  "**/*.ts",
  "**/*.tsx",
  "**/*.d.ts",
  ".next/types/**/*.ts"
]
```

当前配置的 `"**/*.d.ts"` 理论上已经覆盖该文件，所以只有实际验证不通过时才改。

- [ ] **步骤 4: 提交类型接入变更**

运行:

```powershell
git add notion.sync.api.client/next.config.ts notion.sync.api.client/cloudflare-env.d.ts notion.sync.api.client/tsconfig.json
git commit -m "chore: add cloudflare dev types"
```

## 任务 5: 验证 Next.js 原有功能

**文件:**
- 不新增文件。
- 不修改文件，除非验证暴露出 Cloudflare 兼容性错误。

- [ ] **步骤 1: 运行 lint**

运行:

```powershell
pnpm --dir notion.sync.api.client run lint
```

期望:

```text
无 ESLint error
```

- [ ] **步骤 2: 运行原 Next 构建**

运行:

```powershell
pnpm --dir notion.sync.api.client run build
```

期望:

```text
Compiled successfully
```

- [ ] **步骤 3: 运行 OpenNext 构建**

运行:

```powershell
pnpm --dir notion.sync.api.client run cf:build
```

期望:

```text
.open-next/worker.js 已生成
.open-next/assets 已生成
```

- [ ] **步骤 4: 本地预览 Cloudflare 运行时**

运行:

```powershell
pnpm --dir notion.sync.api.client run cf:preview
```

期望:

```text
Wrangler 启动本地预览服务
```

- [ ] **步骤 5: 手动检查核心路由**

在本地预览地址检查:

```text
/
/blog
/tag
/contact
/server-sitemap.xml
/api/cache-orchestrator
```

`/api/cache-orchestrator` 无密钥时应返回:

```text
401 Unauthorized
```

带 `x-revalidation-secret` 且匹配 `LAMBDA_KEY` 时应返回:

```json
{
  "status": "success",
  "count": 0
}
```

`count` 数量以 Supabase 数据为准，不要求固定值。

- [ ] **步骤 6: 提交验证中必要修复**

如果没有修复，不提交。若有兼容性修复，提交:

```powershell
git add notion.sync.api.client
git commit -m "fix: resolve cloudflare preview issues"
```

## 任务 6: 整理部署配置和环境变量清单

**文件:**
- 修改: `notion.sync.api.client/package.json`
- 可选新增: `notion.sync.api.client/.dev.vars.example`

- [ ] **步骤 1: 明确 Cloudflare 需要配置的变量**

在 Cloudflare Workers 项目中配置这些变量或 secret:

```text
HOME_URL
SUPABASE_URL
SUPABASE_SECRET_KEY
LAMBDA_KEY
```

这些变量当前都通过 `process.env.*` 使用，OpenNext Cloudflare 会在 Worker 运行时注入。

- [ ] **步骤 2: 本地开发变量示例**

如果项目当前没有本地变量示例文件，可新增 `notion.sync.api.client/.dev.vars.example`:

```text
HOME_URL=https://example.com
SUPABASE_URL=https://example.supabase.co
SUPABASE_SECRET_KEY=replace-with-service-role-key
LAMBDA_KEY=replace-with-revalidation-secret
```

不要提交真实密钥。

- [ ] **步骤 3: 确认部署命令**

Cloudflare 部署命令:

```powershell
pnpm --dir notion.sync.api.client run cf:deploy
```

如果使用 Cloudflare 控制台连接 Git 仓库，构建命令应配置为:

```text
pnpm run cf:build
```

项目根目录应配置为:

```text
notion.sync.api.client
```

构建输出由 OpenNext 和 Wrangler 管理，不使用传统 Pages static output 目录。

- [ ] **步骤 4: 提交环境示例**

如果新增 `.dev.vars.example`:

```powershell
git add notion.sync.api.client/.dev.vars.example
git commit -m "docs: add cloudflare env example"
```

## 风险点

- `waitUntil` 在 Workers 中有执行时间限制，当前预热批量请求如果 URL 很多，可能被取消。计划阶段不引入 Queue，因为这会超出“尽量复现原功能”的最小改动范围。
- `revalidatePath` 和 `revalidate = 3600` 依赖 OpenNext 的 Next 缓存实现。若实际 Cloudflare 账号没有 R2，自动配置可能不启用持久缓存；需要在验证后决定是否加 R2 cache。
- `next/font/google` 在构建期需要访问 Google 字体。若 Cloudflare 构建环境无法访问，需改为本地字体资源；计划中先保持原样，只有构建失败才处理。
- `@opennextjs/cloudflare` 与 Next.js 15.5.7 的兼容性需以实际 `cf:build` 为准。若 adapter 报版本限制，应按错误信息选择兼容版本，不做无根据降级。

## 完成标准

- `rg "@vercel|vercel" notion.sync.api.client` 无输出。
- `pnpm --dir notion.sync.api.client run lint` 通过。
- `pnpm --dir notion.sync.api.client run build` 通过。
- `pnpm --dir notion.sync.api.client run cf:build` 生成 `.open-next/worker.js` 和 `.open-next/assets`。
- `pnpm --dir notion.sync.api.client run cf:preview` 可访问核心页面和 API。
- Cloudflare 部署环境中配置 `HOME_URL`、`SUPABASE_URL`、`SUPABASE_SECRET_KEY`、`LAMBDA_KEY`。
