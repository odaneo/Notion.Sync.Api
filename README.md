# Obsidian 博客同步迁移

当前正在 `codex/obsidian-sync-assessment` 分支分阶段迁移：

```text
Obsidian Published: true → GitHub Actions → .NET → Supabase PostgreSQL
                                                    ↓ 入库成功后
                                          Next.js / Cloudflare 缓存失效
```

目前已实现 DB 阶段：直接修改现有 Models 和 AppDbContext，沿用 EF Core Migrations。文章属性与 Markdown 合并到 Articles，文章与 Tag、SubTag 分别多对多，两类分类没有父子关系。

- 开发：同一 Supabase 项目的 `blog_test`，账号 `blog_test_writer`。
- 生产：计划使用 `blog`、`blog_writer`，本阶段没有执行生产迁移。
- 日常/迁移连接均为 Session Pooler（5432），分别使用 `Supabase` / `SupabaseMigration` 凭据。
- .NET 开发密码保存在约定的 User Secrets，普通配置放在 appsettings；前端本地统一使用 `.env.local`。生产敏感值分别放在 AWS Secrets Manager 和 Worker Secrets。

```powershell
dotnet build .\Notion.Sync.Api
dotnet ef migrations has-pending-model-changes --project .\Notion.Sync.Api -- --environment Development
dotnet ef database update --project .\Notion.Sync.Api -- --environment Development
```

迁移由设计时工厂使用管理员连接执行，普通 API 不自动迁移。测试 schema 已应用全部九个迁移，关系、约束、只读权限及环境写入隔离验证通过；当前没有真实文章数据。

Supabase API 已启用 `blog_test`，publishable key 已完成验证：五张业务表 GET 均为 200，写请求被拒绝，同步记录与迁移历史不可读。

旧后端 Notion 定时同步、KV 发布及旧文章查询入口已经移除；新 Obsidian 同步接口、Markdown 前端和 Actions 在后续阶段接入。旧 Node helper、前端 KV 及旧 Docker 部署配置尚未切换，本分支目前不用于部署生产。

- [配置位置、读取入口与使用时机](docs/configuration.md)
- [数据库说明与验证](docs/database.md)
- [完整分阶段计划](docs/plans/2026-11-02-obsidian-sync-migration.md)
