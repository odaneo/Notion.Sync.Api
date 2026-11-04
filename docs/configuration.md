# 配置

运行流程见 [README](../README.md)。本地与线上使用相同配置键，值和保存位置按环境区分。

## 本地环境

### .NET 从哪里读取

环境为 `Development`；配置按以下顺序读取，后者可覆盖前者：

```text
appsettings.json → appsettings.Development.json → User Secrets → 环境变量 / 启动参数
```

| 内容 | 唯一维护位置 |
| --- | --- |
| 连接模板、Session Pooler 地址、CA、内容仓库和分支 | `Notion.Sync.Api/appsettings.json` |
| 测试 schema `blog_test`、本机刷新地址 | `Notion.Sync.Api/appsettings.Development.json` |
| writer / 迁移账号与密码、两个 Token | 下方指定的 `secrets.json` |
| Docker 的 Development 环境、5021 端口和宿主机回调地址 | 根目录 `docker-compose.yml` |

User Secrets 文件：

```text
C:\Users\...\secrets.json
```

保留以下分组；密码和 Token 的实际值只填在这个文件中：

```json
{
  "Supabase": { "username": "", "password": "" },
  "SupabaseMigration": { "username": "", "password": "" },
  "Sync": { "Token": "" },
  "Revalidation": { "Token": "" }
}
```

- `Sync:Token`：本地调用者 → .NET；`Revalidation:Token`：.NET → 刷新接口。两个 Token 分开，长度均为 32–512 字符。
- `sync-local.cjs` 自动读取这里的两个 Token，并以启动参数把临时 .NET 的回调地址改为模拟服务；不会修改配置文件。
- Docker 只读挂载同一份 User Secrets；通过 `Revalidation__Url` 指向 `http://host.docker.internal:3000/api/revalidate`。
- 后端密钥只在这里维护；前端所需的刷新 Token 按下表配对。已有 `Kestrel:Certificates:Development:Password` 仅供本地 .NET HTTPS，不配置到 AWS。

### 前端从哪里读取

前端只维护 `notion.sync.api.client/.env.local`，字段参照同目录 `.env.example`；已有文件直接补项。

| 变量 | 本地值 / 来源 |
| --- | --- |
| `HOME_URL` | `http://localhost:3000` |
| `SUPABASE_URL` | 当前 Supabase 项目的 HTTP API 地址 |
| `SUPABASE_SCHEMA` | `blog_test` |
| `SUPABASE_PUBLISHABLE_KEY` | Supabase 项目的 Publishable key |
| `REVALIDATION_TOKEN` | 与上面 User Secrets 的 `Revalidation:Token` 相同 |
| `CACHE_PURGE_ZONE_ID`、`CACHE_PURGE_API_TOKEN` | 本地留空 |

`pnpm dev` 读取 `.env.local`；本地 Worker 预览也使用 Next.js 的环境文件，不另建 `.dev.vars`。系统已有同名环境变量时会优先使用，应避免重复设置。

日常 `pnpm dev` 不需要连接或登录 Cloudflare，仍会查询远端 Supabase。本地刷新接口也校验 Token；通过 localhost 的 Worker 回调跳过线上 Purge。Worker 缓存验证命令见 [README](../README.md#本地环境)。

## 线上环境

### AWS .NET 从哪里读取

普通配置位于 `appsettings.json` 和 `appsettings.Production.json`，包括 `blog`、刷新 URL、AWS Region 和 Secret 名称。密码与 Token 从 AWS Secrets Manager 读取；环境变量 / 启动参数可覆盖配置。

| AWS Secret Manager | 保存的 JSON 字段 | 对应本地配置分组 | 谁读取 |
| --- | --- | --- | --- |
| `notion-api-supabase` | `username`、`password` | `Supabase` | 日常 API |
| `notion-api-supabase-migration` | `username`、`password` | `SupabaseMigration` | `--migrate` 进程 |
| `notion-api-obsidian-sync` | `Token` | `Sync` | 日常 API |
| `notion-api-revalidation` | `Token` | `Revalidation` | 日常 API |

AWS Secret Manager 的字段对应本地 JSON 同名分组。容器运行身份需要相应 Secret 的读取权限；正常 API 不读取迁移 Secret。

Secret 名称已在 `appsettings.Production.json` 的 `AWS:SecretName*` 中定义，沿用现有名称时不必再声明一遍。前端刷新地址为该文件的 `Revalidation:Url`。正常 API 启动时校验两个 Token（32–512 字符）及回调地址（非本机 HTTPS）；缺失或非法会启动失败。独立的 `--migrate` 入口不做这些刷新配置校验。

### 内容仓库 GitHub Actions 从哪里读取

仓库 `odaneo/obsidian-blog-content` → Settings → Secrets and variables → Actions → Repository secrets：

| Secret | 值 |
| --- | --- |
| `SYNC_API_URL` | 完整 HTTPS 同步接口地址，以 `/api/sync/obsidian` 结尾 |
| `SYNC_TOKEN` | 与 AWS `notion-api-obsidian-sync` 的 `Token` 相同 |

这两个值只供工作流调用后端。数据库密码、前端刷新 Token 不交给内容仓库 Actions；重试规则见 [README](../README.md#3-谁负责重试)。

### Cloudflare 前端从哪里读取

沿用现有 GitHub 自动构建。根目录为 `notion.sync.api.client`，构建命令 `pnpm cf:build`，部署命令 `pnpm cf:deploy`。

| 变量 | 生产值 / 来源 | 设置位置 |
| --- | --- | --- |
| `HOME_URL` | `https://odaneo.com` | 构建变量 + Worker 运行时变量 |
| `SUPABASE_URL` | 当前 Supabase 项目的 HTTP API 地址 | 构建变量 + Worker 运行时变量 |
| `SUPABASE_SCHEMA` | `blog` | 构建变量 + Worker 运行时变量 |
| `SUPABASE_PUBLISHABLE_KEY` | 项目的 Publishable key | 构建变量 + Worker 运行时变量 |
| `REVALIDATION_TOKEN` | 与 AWS `notion-api-revalidation` 的 `Token` 相同 | Worker Secret |
| `CACHE_PURGE_ZONE_ID` | `odaneo.com` 的 Zone ID | Worker 运行时变量 |
| `CACHE_PURGE_API_TOKEN` | 仅有该 Zone 的 Cache Purge 权限的 API Token | Worker Secret |

构建变量位于 Settings → Build → Variables and secrets；运行时变量和 Secrets 位于 Settings → Variables and Secrets。前四项构建、运行时都要提供相同值，因为构建会读取数据并预渲染；两个敏感 Token 只放运行时。`keep_vars` 会保留控制台中的运行时变量。

Publishable key 配合数据库的只读授权和 RLS；前端不用 `SUPABASE_SECRET_KEY`，也不用数据库账号密码。

### Cloudflare 实际使用的组件

| 组件 / 绑定 | 用途 |
| --- | --- |
| Worker + OpenNext | 运行 Next.js SSR 和刷新接口 |
| Static Assets / `ASSETS` | JS、CSS、字体和头像 |
| R2 / `NEXT_INC_CACHE_R2_BUCKET` | 保存 Next.js 数据与页面缓存，桶为 `notion-sync-api-client-cache` |
| Durable Object / `NEXT_TAG_CACHE_DO_SHARDED` | 记录缓存失效时间 |
| Cache API 区域缓存 | 加速读取 R2 缓存；同步回调时经 Purge API 清理 |

文章、列表、分类和 sitemap 使用长期缓存，不再按 4 小时或 300 秒更新业务数据。区域副本失效后可回读 R2；部署或缓存缺失时仍可能重新生成。刷新完成表示失效已确认，页面在下一次访问时生成，不主动更新已打开的浏览器。

### 部署后验证与清理

1. Supabase Data API 的 Exposed schemas 包含 `blog`；本地需要的 `blog_test` 保留。前后端必须指向同一环境的 schema。
2. AWS、GitHub、Cloudflare 按上述位置配置，两个调用链的 Token 分别配对。
3. 部署后先访问文章形成缓存，再修改并提交一篇已发布文章，验证 Actions 成功、`SyncRuns.RequiresRevalidation=false`，重新访问页面能看到新内容。跨地区刷新需在线上验证；无变化且无待刷新任务的手动运行会跳过刷新，不能用来验证 Purge。
4. 新链路确认后，删除 Worker 中的 `SUPABASE_SECRET_KEY`、`NEXT_CACHE_DO_QUEUE_*`、`MAX_REVALIDATE_CONCURRENCY`；旧 `BLOG_CACHE` KV 无其他使用者时可删除 namespace，AWS `notion-api-cloudflare-kv` Secret 也不再使用。旧 public 函数见[数据库说明](database.md#supabase-api-权限)。

旧 KV、自调用绑定和刷新队列已从前端配置移除，部署会通过 Durable Object 迁移替换旧队列。
