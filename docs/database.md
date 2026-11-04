# 数据库

两个环境连接同一个 Supabase 项目，用 schema 隔离。表结构来自现有 `Models`、`AppDbContext` 和 `Migrations`；密钥位置见[配置说明](configuration.md)。

## 本地环境

| 用途 | 使用身份 / schema |
| --- | --- |
| 本地 .NET 日常读写 | `blog_test_writer`，`blog_test` |
| 本地 Next.js 读取 | Publishable key，`SUPABASE_SCHEMA=blog_test` |
| 执行迁移 | `postgres`，凭据来自 User Secrets 的 `SupabaseMigration` |

本地同步脚本会真实修改远端 `blog_test`，不会写生产 `blog`。

### 什么时候需要迁移

**只有需要应用新的表结构迁移时才执行；日常启动、同步文章或修改前端不需要。** 在项目根目录执行已有迁移：

```powershell
dotnet run --project .\Notion.Sync.Api -- --migrate --environment Development
```

它使用迁移连接更新 `blog_test`，不会启动 API，也不会同步文章或调用前端。普通 API 启动不自动迁移。

### 数据库专项测试

普通后端测试不要求清空业务表。只有要验证真实数据库的并发、回滚和刷新重试，并且 `blog_test` 业务表为空时，才在 PowerShell 临时开启：

```powershell
$env:RUN_DATABASE_TESTS = "1"
dotnet test .\tests\Notion.Sync.Api.Tests
Remove-Item Env:RUN_DATABASE_TESTS
```

测试会写入并清理样例，已有文章的测试库不要运行这一项。

## 线上环境

| 用途 | 使用身份 / schema |
| --- | --- |
| 生产 .NET 日常读写 | `blog_writer`，`blog` |
| 生产 Next.js 读取 | Publishable key，`SUPABASE_SCHEMA=blog` |
| 执行迁移 | `postgres`，凭据来自 AWS `notion-api-supabase-migration` |

### 有新迁移时怎么执行

后端镜像由现有发布流程和 Function 更新。新版本容器运行后，在已登录的 EC2 终端手动执行：

```bash
docker exec -e AWS_REGION=ap-northeast-1 notion-sync-api \
  dotnet /app/Notion.Sync.Api.dll --migrate --environment Production
```

看到 `Database migrations completed.` 表示已有迁移执行完成。命令只应用尚未执行的迁移；正常服务仍使用 `blog_writer`。迁移进程只读取管理员 Secret，不依赖前端地址或刷新 Token。

这一步只更新数据库结构和权限。导入文章由内容仓库 Actions 调用同步接口完成，见 [README](../README.md#线上环境)。

## 两个环境相同的约定

- 连接使用 Session Pooler 的 5432 端口、`postgres` 数据库；schema 和迁移历史随环境选择。
- SQL 角色名为 `blog_test_writer` / `blog_writer`；池化连接的 `username` 带项目后缀，完整值保存在相应密钥中。
- `DbContext:ConnectionString` 配合 `Supabase` 凭据供日常使用；`DbContext:MigrationConnectionString` 配合 `SupabaseMigration` 凭据供迁移使用。两个环境均校验 TLS，使用仓库内公开 CA `Certificates/supabase-ca.crt`。
- `postgres` 负责迁移和授权；writer 只读写业务表，不负责建表。

### 表和内容规则

| 表 | 内容 |
| --- | --- |
| `Articles` | 仅保存发布文章的属性与 Markdown；`Slug`、`SourcePath` 唯一 |
| `Tags`、`SubTags` | 两套独立分类，没有父子关系 |
| `ArticleTags`、`ArticleSubTags` | 文章与两类分类分别多对多关联，允许无分类 |
| `SyncRuns` | 记录入库及缓存刷新状态 |
| `__EFMigrationsHistory` | 记录该 schema 已执行的迁移 |

文章、Tag、SubTag 使用笔记中的固定 UUID `Id`；改名、移动或修改 Slug 时保留 Id。`TagRefs` 只在 `Tags` 中查找，`SubTags` 只在 `SubTags` 中查找，兼容短链接、路径及别名；找不到则忽略。

### SyncRuns 怎么看

| 字段 | 业务含义 |
| --- | --- |
| `Repository` + `CommitSha` | 唯一标识一次源版本；同一提交多次调用更新同一行，不会每次新增记录 |
| `WorkflowRunNumber` | 同步运行序号，防止旧请求覆盖新数据；本地脚本使用测试库当前最大值 + 1 |
| `DatabaseAppliedAt` | 该版本入库已提交的时间 |
| `RequiresRevalidation` | true 表示仍有缓存刷新未确认完成；false 表示没有未完成刷新任务 |
| `CacheInvalidatedAt` | 缓存刷新确认成功的时间 |
| `LastError` | 最近的同步或刷新错误 |

数据无变化不代表一定不刷新；已有待刷新任务会补刷。失败处理和 Actions 重试统一见 [README](../README.md#2-什么时候刷新失败会怎样)。

### Supabase API 权限

Data API 暴露需要访问的 schema，Next.js 通过 `Accept-Profile` 选择 `blog_test` 或 `blog`。Publishable key 使用 `anon` 的只读权限：可查询五张内容表，不能修改，也不能读取 `SyncRuns` 或迁移历史；授权和 RLS 由迁移配置。

新前端直接查询表关系，不调用旧 public 博客函数。确认旧前端下线且无其他调用者后，可删除旧函数；迁移不会自动删除 public 中的旧对象。
