# Obsidian 博客

本文按本地、线上说明运行步骤、调用关系和失败恢复。配置、数据库各自集中维护：

- [configuration.md](docs/configuration.md)：配置填在哪里、由谁读取。
- [database.md](docs/database.md)：使用什么账号、何时迁移、如何查看同步记录。

## 本地环境

本地需要 .NET 8、Node.js 22、pnpm 10。先完成[本地配置](docs/configuration.md#本地环境)；首次拉取后执行 `dotnet restore .\Notion.Sync.Api`。

本地使用远端 Supabase 的 `blog_test`，不启动本地 PostgreSQL，不读取 AWS Secrets Manager。

### 1. 同步笔记并校验后端

在项目根目录手动执行：

```powershell
node .\scripts\sync-local.cjs
```

调用关系：

```text
运行脚本
  → 读取本地 Obsidian 工作区，打包 Published: true 的文章
  → 自动启动临时 .NET 服务，使用 User Secrets 中的 Sync:Token 调用同步接口
  → .NET 按差异写入 Supabase.blog_test
  → .NET 调用脚本创建的模拟刷新接口
  → 脚本校验失败重试、重复请求跳过，随后关闭临时服务
```

- 默认笔记目录为当前用户的 `Documents\Obsidian`，包含尚未提交的修改。
- 自定义目录：`node .\scripts\sync-local.cjs --vault "D:\Notes\Obsidian"`，目录应为包含同步脚本的内容仓库工作区。
- 不需要先启动 Docker、常驻 .NET 或 Next.js；脚本自己启动测试所需的服务。
- 有刷新任务时，模拟接口第一次失败、第二次成功；没有变更也没有待刷新任务时，校验跳过刷新。
- **这条命令会真实更新 `blog_test`，但不会通知 Next.js，也不会清理 Cloudflare 缓存。** 本地记录变为 `RequiresRevalidation=false` 只表示模拟回调完成。
- 启动或数据库错误查看 `.dotnet-home/sync-local.log`。

### 2. 启动前端看页面

在前端目录执行：

```powershell
cd .\notion.sync.api.client
pnpm install --frozen-lockfile
pnpm dev
```

浏览器访问 `http://localhost:3000`，文章地址为 `/blog/<slug>`。Next.js 从 `.env.local` 读取配置，通过 Supabase API 查询 `blog_test` 并服务端渲染；展示文章不依赖 .NET 持续运行。

前一节的同步脚本使用模拟回调。脚本更新数据后，在项目根目录执行以下命令，让已经启动的前端立即重读数据：

```powershell
node .\scripts\revalidate-local.cjs
```

脚本自动读取前端 `.env.local` 的 `REVALIDATION_TOKEN`，调用 `http://localhost:3000/api/revalidate`。输出 `{"revalidated":true}` 后重新访问页面。

Worker 预览也用同一脚本，在项目根目录传入预览端口（以 `cf:preview` 终端输出为准）：

```powershell
node .\scripts\revalidate-local.cjs 8787
```

手动验证失效：先访问预览中的文章形成缓存，再修改笔记并运行 `sync-local.cjs` 入库；此时重新访问仍显示旧内容。执行上面的刷新命令后，再访问应显示新内容。验证期间保持同一个预览进程运行，不重新构建，以免构建本身更新缓存。

需要常驻 .NET 进行接口联调时，在项目根目录二选一：

```powershell
docker compose up --build
```

或：

```powershell
dotnet run --project .\Notion.Sync.Api --launch-profile http
```

两者都提供 `http://localhost:5021/api/sync/obsidian`，仍需 `Sync:Token`。常驻服务收到手动提交的合法完整快照后，会按线上规则执行差异入库，并在需要时通知本地 Next.js；原生 .NET 回调 `localhost:3000`，Docker 通过 `host.docker.internal:3000` 访问宿主机前端。

### 3. 本地能验证到哪里

| 内容 | 验证方式 |
| --- | --- |
| 后端解析、差异写入、模拟刷新重试 | 根目录运行 `node .\scripts\sync-local.cjs` |
| 后端测试 | 根目录运行 `dotnet test .\tests\Notion.Sync.Api.Tests` |
| 页面、Markdown、表格、代码高亮、Mermaid | 前端目录运行 `pnpm dev` |
| 前端读取与认证测试 | 前端目录运行 `pnpm test` |
| 实际 Worker 的缓存命中、主动失效、新增/删除、sitemap | 前端目录依次运行 `pnpm cf:build`、`pnpm test:worker`；使用本地模拟数据 |
| 手动查看 Worker 预览 | 前端目录运行 `pnpm cf:preview`，地址看终端输出；R2、DO 默认本地模拟 |

`next dev` 的热更新和缓存行为不能代替生产缓存验证。Windows 的 Worker 打包需要符号链接权限（开启Windows开发者模式）或使用 WSL。

真实的跨地区缓存清理、GitHub Runner 请求是否被 Cloudflare 拦截、EC2 运行身份及自动更新镜像，需要在线上验证；本地测试不覆盖这些外部链路。

## 线上环境

### 1. 内容发布：提交后自动同步

```text
笔记提交到内容仓库 main
  → GitHub Actions 打包完整发布快照
  → HTTPS POST 到 SYNC_API_URL（Bearer SYNC_TOKEN）
  → Cloudflare 入口 → Nginx → AWS .NET /api/sync/obsidian
  → .NET 提交 Supabase.blog 的差异更新
  → 需要刷新时，.NET 调用 Next.js /api/revalidate（Bearer Revalidation:Token）
  → Next.js 标记缓存失效，并调用 Cloudflare Purge API
  → 刷新确认成功，.NET 清除待刷新标记，Actions 完成
```

- 内容仓库为 `odaneo/obsidian-blog-content`。`main` 的 Markdown、同步脚本或工作流发生变化时自动触发；合并到 `main` 也属于 push。
- 也可在 Actions 中手动选择 `main`，点击 **Run workflow**。工作流只处理已提交的笔记。
- 只收录布尔值 `Published: true`；改成 false 或删除文件，会删除库中旧文章及关联。
- .NET 校验 Token、仓库和分支后处理完整清单；按 UUID、内容 Hash 和属性比较，只写有差异的内容。
- Actions 排队执行；数据库事务锁和 `WorkflowRunNumber` 防止旧请求覆盖新版本。过时请求返回 `superseded`，不再写库或刷新。
- 浏览器访问 Next.js Worker；缓存缺失或失效时，Next.js 从 Supabase API 读取 `blog` 并渲染。

### 2. 什么时候刷新，失败会怎样

`RequiresRevalidation` 表示**这次入库是否还有未完成的缓存刷新任务**。

| 本次业务数据 | 是否已有待刷新任务 | 同步接口的行为 |
| --- | --- | --- |
| 有变化 | 任意 | 刷新 |
| 没变化 | 有 | 补刷 |
| 没变化 | 没有 | 跳过刷新 |

业务变化包含文章、Tag、SubTag 及分类关联。服务端先在事务中保存 `RequiresRevalidation=true` 并提交，再请求刷新：

- 确认成功：改成 `false`，写入 `CacheInvalidatedAt`、清除 `LastError`。
- 刷新失败：文章已经入库，不回滚；标记保持 `true`，记录错误，返回 `503 / cache_pending`。页面可能继续显示旧缓存。
- 同一快照重试只补未完成的工作，不重复新增文章；`status=unchanged` 也可能伴随 `cacheRefreshed=true`。
- 只有收到刷新接口的 `{"revalidated":true}` 才算完成。后续较新版本刷新成功，也会确认此前版本的待刷新任务。

标记本身不会启动后台任务；重试由下节的调用方发起。若标记已是 `false`，且下次同步没有变化，就会跳过刷新。

### 3. 谁负责重试

重试由 GitHub Actions 中的 `send-snapshot.cjs` 发起，不是 GitHub 自动重跑整个工作流。

| 情况 | 处理 |
| --- | --- |
| 网络异常、`429`、`5xx`（包含 `503 / cache_pending`） | 最多再试 3 次，加上首次共 4 次；等待间隔为 2、5、10 秒 |
| 其他 `4xx` | 立即失败，先修正数据、认证或入口配置 |
| 4 次仍未成功 | 工作流失败；修复原因后手动 **Run workflow（main）**，或等待后续提交触发 |

重试发送相同快照。接口的 `Retry-After: 5` 是建议，当前脚本按上表间隔重试。

### 4. 代码部署与内容同步分开

| 操作 | 手动做什么 | 之后自动做什么 |
| --- | --- | --- |
| 发布文章 | 提交内容仓库 `main` | Actions → .NET → 数据库 → 缓存刷新 |
| 部署后端代码 | 在 API 仓库手动触发 `Push image to ECR` | 构建镜像、推送 ECR，既有 AWS Lambda 更新 EC2 容器 |
| 部署前端代码 | 推送到 Cloudflare GitHub 集成绑定的部署分支 | Cloudflare 自动构建、部署 Worker |
| 更新数据库表结构 | 有新迁移时执行[数据库说明](docs/database.md)中的命令 | 应用尚未执行的迁移；不会自动同步文章 |

线上配置来源和首次上线步骤见[配置说明](docs/configuration.md#线上环境)。

### 5. 判断同步结果

| 现象 | 查看哪里 |
| --- | --- |
| `503 / cache_pending` | `SyncRuns.LastError`、.NET 日志、Worker 刷新日志；检查刷新 Token 和 Purge 配置 |
| `403` 且日志有 `cfMitigated=challenge` | Cloudflare 安全事件；请求可能尚未到达 .NET |
| 同步接口 `401` | 核对 GitHub `SYNC_TOKEN` 与 AWS `Sync:Token` |
| 数据库错误的 `503` | .NET 日志；它与 `cache_pending` 是不同错误 |

EC2 查看后端日志：

```bash
docker logs --since 10m --tail 100 notion-sync-api
```
