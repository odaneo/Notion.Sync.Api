# Obsidian 博客同步迁移计划

日期：2026-11-02

更新：2026-11-03，确认发布规则、Session Pooler（5432）连接方式及无图片范围，补充阶段交付与验收；日常连接与迁移连接分别配置，线上线下保持相同配置结构。

工作分支：`codex/obsidian-sync-assessment`

状态：DB 阶段已实现并应用到 Supabase `blog_test`，数据库关系、权限及 Supabase REST 只读验证通过。新同步接口、前端改造与生产部署尚未开始。

## 目标与范围

- 数据源改为本地 Obsidian，经 Git 提交到 GitHub 后触发同步。
- 唯一发布条件为 YAML frontmatter 中的布尔值 `Published: true`。`Published: false` 或没有该字段的笔记不进入同步内容包，不写入数据库，也不处理其正文和其他文章属性。
- GitHub Actions 调用 AWS 上的 .NET 服务；.NET 提交数据库事务后，主动调用 Next.js 刷新接口。
- Next.js 服务端通过 Supabase REST API 获取数据，渲染 Markdown。
- 开发和生产使用同一 Supabase 项目，使用不同 schema 和数据库权限。
- 博客内容长期缓存，仅由同步更新流程主动失效，取消四小时定时同步、定时页面刷新及其他内容刷新入口。
- 彻底删除旧 Notion 同步实现，不保留兼容分支、旧接口、注释掉的实现或 KV 回退读取。
- 本次迁移不改项目的功能定位和主要页面布局；项目/仓库名称可暂时保留，名称不代表保留 Notion 功能。
- 第一版没有图片和附件，不引入图片上传、Storage bucket 或附件发布服务。

```text
Obsidian → Git push → GitHub Actions
  → HTTPS POST .NET 同步接口（同步密钥）
  → 校验完整的 Published: true 文章清单及内容包
  → 事务更新 Supabase PostgreSQL
  → HTTPS POST Next.js 刷新接口（另一把刷新密钥）
  → 失效博客数据、页面及相关 Cloudflare 缓存
  → 刷新确认成功后，同步接口返回成功

用户访问 → Next.js 缓存
  → 缓存缺失或主动失效后：Supabase API → Markdown 渲染 → 写入缓存
```

## 一、密钥与环境约定

### 唯一允许的持久化位置

| 环境/使用方 | 保存位置 | 说明 |
| --- | --- | --- |
| 开发 .NET | `C:\Users\odane\AppData\Roaming\Microsoft\UserSecrets\af598fa0-4a2d-4362-8530-a0c49add5792\secrets.json` | 保留现有 `UserSecretsId`，只读取开发凭据 |
| 开发 Next.js / 本地 Worker 预览 | `notion.sync.api.client/.env.local` | 不进入 Git；仅服务端读取敏感值 |
| 生产 .NET | AWS Secrets Manager | 通过 AWS 运行身份读取，不把密钥写进镜像或服务器配置文件 |
| 生产 Next.js | Cloudflare Worker Secrets | 从 Worker 运行时 binding 读取，不写入 `wrangler.jsonc` 的 `vars` 或前端构建产物 |
| GitHub Actions | 执行时从 AWS Secrets Manager 读取 | 不在 GitHub Secrets 再保存生产业务密钥；只在任务进程中临时使用 |

开发密钥不分散到 `.env`、`.dev.vars`、`appsettings*.json` 或脚本里。合并本地必需配置到 `.env.local` 后，删除旧 `.env` / `.dev.vars` 中的凭据与过时文件，确认 Wrangler 本地预览确实读取 `.env.local`。[本地 secrets 加载规则](https://developers.cloudflare.com/workers/configuration/secrets/)

同步密钥和刷新密钥分开，开发与生产使用不同的值。地址、schema 名、AWS Region、Secret ARN 和角色 ARN 等非敏感配置可以放入正常配置文件或 GitHub Variables。

### 线上线下使用同一配置结构

配置名称、保存位置、读取入口和使用时机统一见 [配置说明](../configuration.md)，本计划不另维护一套配置清单。

2026-11-03：测试 writer 与迁移管理员已分别配置独立密码。按用户要求，User Secrets 进一步缩减为密码等敏感值；用户名、连接模板、schema、公开 CA 证书均移到普通配置。开发证书密码保留，旧 Notion/KV 本地密钥清理。AWS 生产 Secret 尚未修改，上线切换时按同样结构整理。

两个环境使用相同业务键和层级：运行连接读取 `Supabase`，EF 迁移读取 `SupabaseMigration`，共用 `BuildFinalConnString`。用户名来自 appsettings，密码来自对应 Secret，普通服务不读取生产迁移 Secret。

前端本地仅保留 `.env.local`。原 `SUPABASE_READ_KEY` 改名为 `SUPABASE_PUBLISHABLE_KEY`，不保留别名；它是 Supabase HTTP API 的长期配置，不是数据库账号或一次性建表凭据。只读限制由表授权与 RLS 决定。新的同步/刷新 token 在对应阶段实施时才配置，不提前占位。

GitHub Actions 复用仓库现有的 AWS OIDC 登录方式，使用只允许读取指定同步密钥的 IAM 角色，按精确 Secret ARN 调用 `GetSecretValue`。信任条件限定笔记仓库、发布分支和生产运行环境；必要的 KMS 权限也限定到对应密钥。读取结果立即掩码，不输出到日志、工作区或 artifacts。[AWS 官方工作流](https://docs.aws.amazon.com/secretsmanager/latest/userguide/retrieving-secrets_github.html)

这里的 OIDC 只解决 Actions 如何取得 AWS 短期身份和读取密钥；.NET HTTP 接口仍使用简单的共享密钥验证，无需增加 API Gateway、Cloudflare Access 或自建 OIDC 验证模块。

## 二、DB 计划

- [x] 日常同步使用用户确认的 Supabase Shared Session Pooler：`aws-1-ap-northeast-1.pooler.supabase.com:5432`，数据库 `postgres`，项目标识 `uwxiqiasdfhorahxqvmp`；管理员密码沿用用户提供的值，测试 writer 密码独立生成并保存到 User Secrets。
- [x] 配置生产 `blog`、开发 `blog_test`，连接同一 Supabase 项目；schema 由部署配置固定。当前只创建 `blog_test`，没有执行生产迁移。
- [x] 直接更新原有 `Article`：合并属性与 Markdown 正文（沿用 `Content` 字段），不存储草稿或 false 状态。
- [x] 为 33 篇带博客属性的文章、9 个 Tag、31 个 SubTag 补齐固定 UUID，保存在笔记属性 `Id` 中，直接作为对应数据库主键；同步时保留该值，改名、移动或修改 Slug 不改变 Id。Slug 和 SourcePath 仍保持唯一。
- [x] 更新现有两张关联模型为 `ArticleTag`、`ArticleSubTag`，分别支持文章与两类分类的多对多关系。
- [x] 删除 `SubTag.TagId`、父子导航和外键；共享子标签、多标签关系已通过事务内验证。
- [x] 新增 `SyncRun`，按仓库和提交版本唯一，分别保存入库与刷新结果，禁止先记录刷新成功再记录入库成功。
- [x] 继续使用原有 `AppDbContext` 和 `Migrations`。新增 `20261103014108_ObsidianArticles`，在 `blog_test` 应用了原有八个迁移及本次迁移，历史表位于同一 schema；没有新模型目录、独立迁移工具或第二套 SQL 版本表。
- [x] 两条连接按用途使用独立凭据；通过 Npgsql `Search Path` 选择目标 schema，EF 迁移历史显式指定 schema。Session Pooler 已验证支持此方式，模型不需按环境复制或重写 schema。
- [x] 开发/生产统一连接模板和凭据键，缺失时明确报错；运行连接禁止使用管理员账号，不回退到迁移连接。EF 设计时工厂只读取迁移凭据，普通服务不自动迁移。
- [ ] 迁移使用已有 `postgres` 管理员账号，不额外新增第四个数据库账号。迁移凭据保存在独立 Secret，生产服务运行身份不能读取它；仅显式迁移执行身份拥有读取权限，日常同步 Actions 也不能读取迁移凭据。
- [x] `blog_test_writer` 已创建并验证可登录，只能读写测试业务表，不能建表或写入生产/public 表；RLS 与 anon 只读权限通过数据库验证，SyncRuns/迁移历史不向 anon 开放。[Supabase 权限依据](https://supabase.com/docs/guides/api/securing-your-api)
- [x] Data API 的 Exposed schemas 已启用 `blog_test`；publishable key 访问五张业务表均返回 200，写入被拒绝（42501），SyncRuns 与迁移历史不可读。
- [ ] 旧生产表的清理脚本与新表创建分开：在新链路验证通过、前端切换后，清理旧 Notion 表及其关联；不将旧 recordMap JSON 当作 Markdown 导入。

第一版同步接口直接等待入库和刷新结果，不需要 Hangfire。旧定时任务、Hangfire 存储与 Dashboard 一并移除；如果实现中确认存在必须使用后台任务的实际需求，再使用生产 `hangfire` / 开发 `hangfire_test`，不能连接生产任务队列。

### 连接模板（密码留空）

以下仅展示无密码模板。DB 实施已使用约定的 User Secrets 真实凭据连接并迁移测试 schema，文档不包含真实密码。

```text
postgresql://postgres.uwxiqiasdfhorahxqvmp:@aws-1-ap-northeast-1.pooler.supabase.com:5432/postgres
```

对应 .NET 配置沿用现有生产环境的占位符格式。模板和用户名保存在 appsettings，密码分别填入 User Secrets 的 `Supabase:password` 与 `SupabaseMigration:password`；不再将普通配置复制到秘密文件。具体结构统一见 [配置说明](../configuration.md)。两条模板中的 `{USERNAME}` / `{PASSWORD}` 保持不变，TLS 使用随应用发布的公开 CA 验证。

上述统一读取已在现有扩展方法和 `AppDbContextFactory` 实现，并通过测试 writer 与管理员分别登录验证；不在普通服务中读取迁移管理员 Secret。

`SupabaseMigration` 使用的 `postgres` 管理员本身不受测试 schema 权限限制，迁移目标须显式限定并在执行前检查；最初创建 schema 和角色可在 Supabase SQL Editor 执行初始化脚本。受限角色连接使用 `[ROLE].uwxiqiasdfhorahxqvmp` 的 Pooler 格式，主机和 5432 端口不变。

已确认的账号安排：保留现有 `postgres` 用于管理与迁移，新建 `blog_writer` 仅读写生产 `blog`、`blog_test_writer` 仅读写开发 `blog_test`，合计三个数据库账号。Next.js 使用 Supabase API 只读身份，不新增数据库登录账号。开发和生产都采用相同的两条连接配置键，分别填入对应环境的值。

Session Pooler 端口及自定义角色用户名依据：[Supabase 连接说明](https://supabase.com/docs/guides/database/connecting-to-postgres)；Npgsql 参数依据：[连接参数](https://www.npgsql.org/doc/connection-string-parameters.html)。

## 三、后端与 GitHub Actions 计划

- [ ] 增加 `POST /api/sync/obsidian`，仅接受认证后的完整已发布文章清单、相应内容包和提交版本；输入限制包括文件类型、大小和安全路径。
- [ ] 从固定的笔记仓库和发布分支取得本次提交。Actions 先按 `Published: true` 筛选，仅打包这些文章及能找到的分类定义，缺失分类引用直接跳过；排除草稿、图片、`.git`、`.obsidian` 等内容。不接受任意远程下载地址或 SQL。
- [ ] 后端再次检查文章的 `Published` 确为布尔值 `true`，才校验固定 `Id` 并解析 Slug、Description、Recommend、TagRefs、SubTags；文章及实际找到的分类 Id 必须有效且无重复，直接使用笔记中的值。标题默认取文件名，编辑时间取对应文件的 Git 提交时间，不采用 checkout 后的文件 mtime。`false` 或缺失字段的笔记直接忽略，不建立草稿同步通道。
- [x] 33 篇文章的分类属性已整理为 YAML 列表；短链接、完整路径及带显示别名的写法均允许，例如 `[[前端]]`、`[[Tags/前端]]`、`[[Tags/前端|前端]]`；空列表为 `[]`，保留原分类 Id。
- [ ] 分类解析分别限定 `TagRefs → Tags`、`SubTags → SubTags`，从引用目标读取固定 Id 建立关系。找不到的引用直接忽略，不报错、不创建分类、不阻断文章同步；无有效分类时关联为空。重复 Id 或 Slug 冲突仍按数据冲突处理。
- [ ] 全量对齐当前 `Published: true` 清单，按哈希更新有变化的文章。只有完整已发布清单及内容包全部校验成功后，才在一个事务中更新文章、分类及关联，并删除已不在清单中的旧发布记录。
- [ ] 同一目标串行处理，防止旧提交覆盖新提交；按提交版本幂等，网络中断后的重试不能重复制造文章。
- [ ] 数据库提交后，调用固定的 Next.js 刷新地址。入库失败不刷新；刷新失败不谎报成功，记录待刷新状态，同一提交重试时继续完成刷新。
- [ ] 同步 API 仅在入库和缓存失效均确认成功后返回成功。Actions 等待结果，失败时保留错误信息并支持重新运行。
- [ ] 在笔记仓库增加 `push` 和 `workflow_dispatch` 工作流；只对发布分支的内容/分类变更执行同步，不对 PR、fork 或 `.obsidian` 工作区状态变更发布内容。
- [ ] 简化后端 Dockerfile，只运行 .NET；部署依赖、启动方式和运行时秘密读取配置随之更新。

接口安全采用 HTTPS、请求头中的独立密钥、恒定时间比较和基本限流。日志仅记录提交、计数及结果，不记录凭据。开发服务启动时校验 schema 和刷新目标，避免开发数据或开发密钥误用生产目标。

“不处理 false”指不上传、不保存、不验证草稿正文。先前已发布的文章改为 false 后，本次完整已发布清单不再包含它，因此删除其旧入库记录和关联，并刷新缓存；不会把该文章作为草稿更新回 DB。完整已发布清单确认为空时可清空本来源已发布记录，内容包缺失或生成失败则不能当作空清单。

## 四、前端与 Cloudflare 计划

- [ ] 新建统一的服务端数据读取模块，用原生 `fetch` 调用 Supabase REST API，显式使用对应 schema。开发也读取 `blog_test`，移除 `NODE_ENV=development` 自动切 mock 的旧行为。分类为空的文章仍正常渲染，不假设必有首个 Tag。
- [ ] 数据读取使用明确的长期缓存配置，并统一标记博客缓存标签；首页、文章列表、分类、正文、metadata 和 sitemap 都走该模块。[Next.js 15 标签失效机制](https://nextjs.org/docs/15/app/api-reference/functions/revalidateTag)
- [ ] 用 Markdown 渲染替换 recordMap / `react-notion-x`，覆盖现有代码块、表格、链接及已发布文章中的 Mermaid；原始 HTML 不直接执行，链接协议需校验。
- [ ] 正文中的 Wiki 链接解析为已发布文章地址；未发布文章不输出私有笔记内容。目录、锚点和含中文/空格的路径纳入验证。
- [ ] 增加 `POST /api/revalidate`，仅由同步服务持有的刷新密钥授权，固定失效博客缓存，不允许请求方提交任意 URL 或切换环境。
- [ ] 取消文章页 `revalidate = 14400`，调整首页/列表/标签页的 `force-dynamic`，使博客页面统一受主动刷新控制；移除 sitemap 的独立 300 秒内容过期策略。
- [ ] 保留 R2 增量缓存，为现有 OpenNext 配置必要的 Tag Cache。博客规模优先使用文档推荐的简单 D1 实现，不增加分片设计；不再需要定时 revalidation queue。[OpenNext 主动失效配置](https://opennext.js.org/cloudflare/caching)
- [ ] 检查实际 Cloudflare Cache API/CDN 缓存层，对存在的相关边缘副本在同一刷新流程中清除；不用全站 purge，也不清理哈希静态 JS/CSS。没有使用的缓存层不额外增加。
- [ ] 刷新接口等待所需失效操作完成才返回成功；页面在后续访问时重新查询并渲染。开发和生产的缓存 key、Worker 及 binding 各自隔离。
- [ ] 生产敏感配置从 Worker 运行时读取；本地开发和 Worker 预览都从 `.env.local` 读取，不使用 `NEXT_PUBLIC_` 暴露敏感值。

日常内容更新仅由上述同步请求触发缓存失效，不增加定时刷新、页面端轮询或额外手动清缓存入口。部署和缓存存储自身的淘汰不属于内容发布触发器；联调需使用生产构建/Worker 预览验证缓存，不能只凭 `next dev` 判断。

## 五、旧逻辑彻底删除清单

| 范围 | 需要删除或替换的现有内容 |
| --- | --- |
| 旧抓取任务 | `Job/NotionDatabaseSyncJobService.cs`、`Common/NotionHelper.cs`；Notion 请求、Lambda 调用和被注释的历史刷新代码 |
| 旧发布任务 | `BlogCachePublisher`、`CloudflareKvClient`、对应接口、DTO、Options 和 DI 注册 |
| Node helper | 整个 `notion.sync.api.node` 项目及其 `.env`、脚本、依赖；`.sln` 中的项目和构建配置 |
| 旧文章模型 | `NotionArticle` 及关联模型、DTO、Profile、Repository、Service；旧正文/元数据双表结构和按 Notion pageId 查询的 `ArticleController` |
| 旧 EF 迁移 | 按用户确认继续保留原有迁移历史，在同一目录新增迁移并更新快照；旧历史不是运行中的同步逻辑，不维护第二套版本管理 |
| 定时调度 | `Program.cs` 的旧 cron 注册，已持久化的 recurring job，以及没有新用途的 Hangfire 包、存储初始化、认证类和 Dashboard |
| 前端数据源 | `src/utils/blog-cache`、旧 KV 类型与 manifest、`BLOG_CACHE` binding；不保留生产 KV 或开发 mock 作为回退 |
| Notion 渲染 | `NotionSSR.tsx`、recordMap 解析、Notion CSS/import、`react-notion-x` / `notion-client` / `notion-types` / `notion-utils` 等直接或传递依赖 |
| Docker / 部署 | Node 安装和 build stage、双进程入口、旧端口、旧 build args、镜像内生成 `.env` 的步骤；更新 AWS Compose 和 ECR 构建工作流 |
| 本地 DB 启动 | 更新 README，移除应用对本地 PostgreSQL/pgAdmin Compose 的依赖和旧启动配置；后续如何处置已有容器/卷单独列明，不影响其他项目 |
| 配置与密钥 | `NotionDatabaseId`、Notion Token/Cookie/User、旧 Lambda 配置、`CloudflareKv` 发布凭据、Node 专属 DB 配置、`LAMBDA_KEY` 等过时键；新的刷新密钥使用新名称 |
| 文档与产物 | 更新 README 和部署文档，删除不再适用的旧 KV/Notion 迁移计划；更新锁文件、Cloudflare 类型和示例配置，清理可能残留旧 bundle 的本地构建目录 |

运行环境也要清理：移除确属本服务的旧任务、失效密钥、旧 KV binding 和旧 Node 进程。旧 KV 数据/namespace 与生产旧表在新链路验收后清理，先确认资源归属；保留 Git 历史不等于保留可运行的旧逻辑。

## 六、实施顺序与验收

按下表分阶段执行，每个阶段都应能单独检查结果；继续沿用本文件的清单，不拆出重复的大量子计划。

| 阶段 | 范围与交付物 | 阶段验收 |
| --- | --- | --- |
| 0：配置与数据契约 | 确定仓库 URL、发布分支、schema；准备空密码配置位置、已发布清单及文章 DTO 契约 | 发布条件固定为 `Published: true`；开发目标指向测试 schema；用户可在约定位置填入密码 |
| 1：DB | 新文章/分类/关联表、同步记录、带版本的迁移 SQL、角色权限及 Supabase 只读 API | 显式迁移入口用迁移连接在 `blog_test` 建表并记录历史；日常连接只允许对应业务读写；两条 5432 Session Pooler 连接可独立配置，生产旧表不受影响 |
| 2：后端 | 发布内容解析、同步接口、事务/幂等、刷新调用与失败重试 | true 可写入；false/缺失字段不入库；完整清单变更能移除旧发布记录；刷新失败时重试不会重复写入 |
| 3：前端 | Supabase 服务端读取、Markdown/Mermaid、长期缓存与刷新接口、OpenNext 配置 | 列表/分类/正文/sitemap 正常；Worker 预览中只有同步请求触发业务缓存失效；开发只读取测试 schema |
| 4：Actions 与清理 | 笔记仓库工作流、AWS 运行时取密钥、纯 .NET 镜像；按清单删除旧实现和配置 | 一次 push 完成同步及刷新；失败可重新运行；没有 Notion、Node helper、KV 发布或旧定时任务的运行时残留 |
| 5：整体与生产验收 | 完成构建检查、完整场景联调和生产切换，执行已核对范围的旧资源清理 | 下面的验收项全部通过，生产源为 Obsidian，密钥与缓存/数据环境隔离符合约定 |

验收场景：

- [ ] `Published: true` 的新增和修改文章入库；false/缺失字段的笔记不入库；true 改为 false 后旧发布记录移除且无草稿记录；删除、改名/改 Slug、分类元数据变更均得到预期结果。
- [ ] 一篇文章同时关联多个 Tag/SubTag；同一个 SubTag 可以出现在不同分类的文章中。
- [ ] 分类引用兼容短链接、完整路径及显示别名；指定目录中找不到的引用静默跳过，即使全部找不到也能保存并渲染文章。
- [ ] 重复提交、旧提交、错误密钥、非法内容包和部分解析失败，不产生重复数据、误删或越权写入。
- [ ] 模拟刷新失败：数据库只应用一次，重试最终完成缓存失效；刷新成功前 Actions 不显示同步成功。
- [ ] Worker 预览中确认：没有定时内容失效；同步后首页、列表、分类、正文与 sitemap 都能读取新版。
- [ ] 开发写入和读取仅作用于 `blog_test`，开发身份不能修改生产 schema，也不会刷新生产缓存。
- [ ] `DbContext:ConnectionString` 使用受限的对应环境角色完成应用读写，`DbContext:MigrationConnectionString` 仅由显式迁移入口使用管理员角色；两条连接均经指定 5432 Session Pooler，能够独立覆盖环境变量，应用不自动迁移或回退到管理员连接。
- [ ] 密钥只位于约定位置；Git、镜像层、浏览器 bundle、日志和 artifacts 中没有敏感值。
- [ ] 用测试值验证 User Secrets JSON 与带前缀的 Secrets Manager JSON 得到同样的业务配置路径；`.env.local` 与 Worker Secrets 的变量名一致，业务服务没有因环境不同而改用另一套键名或结构。
- [ ] 对旧任务类、Node 路径、Notion API/recordMap、KV 发布键、旧配置名和依赖做全仓扫描；结果只允许出现在本计划的清理说明或必要的退役 SQL 中，不存在运行时引用、旧注释实现或兼容路径。

## 已确认输入与尚待提供的信息

已确认：

- 唯一发布条件为 `Published: true`，false 笔记不参与内容同步、不入库。
- Supabase 日常连接采用已确认的 Shared Session Pooler（5432），密码留空，由用户填写。
- 第一版没有图片，不需要图片及附件服务。
- 开发密钥使用指定 User Secrets 与 `.env.local`，生产使用 AWS Secrets Manager 与 Worker Secrets。

已配置的源仓库：

- GitHub 仓库：`odaneo/obsidian-blog-content`；SSH 地址为 `git@github.com:odaneo/obsidian-blog-content.git`。
- 本地 vault：`C:\Users\odane\Documents\Obsidian`，remote `origin` 的 fetch/push 地址均已配置为以上 SSH 地址。
- 发布分支：`main`，与当前 vault 的本地分支一致。
- 本轮仅配置并核对本地 remote，没有提交、推送、设置 upstream 或验证 GitHub SSH 认证。
- 源仓库建议设为私有；源 vault 中非发布笔记的 Git 提交与博客发布筛选是不同的边界。

开发数据库账号和迁移凭据已配置，测试 writer 已创建；publishable API key 和 Exposed schemas 已配置且完成 REST 验证。用户已在 AWS 创建 `notion-api-supabase-migration`，仅保存管理员 `password`，生产 appsettings 已补齐对应名称；尚未从 AWS 读取验证。生产 `blog` / `blog_writer` 尚未创建，未执行生产迁移；开发与生产刷新服务地址留到后端阶段配置。
