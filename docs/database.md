# 数据库开发与迁移

沿用现有 Models、AppDbContext 和 EF Migrations。测试 schema 已应用九个迁移，生产尚未迁移；新同步接口和前端读取尚未接入。

## 账号与连接

日常运行使用：

- 本地 .NET：`blog_test_writer`
- 生产 .NET：`blog_writer`
- Next.js：Publishable key

`postgres` 仅用于迁移、建表和授权等管理操作。writer 只有业务读写权限，没有建表权。

- 同一 Supabase 项目：开发 schema 为 `blog_test`，生产为 `blog`。
- 使用 Session Pooler（5432），通过 `Search Path` 选择 schema，迁移历史也保存在其中。
- TLS 使用 `SSL Mode=VerifyFull` 和项目中的公开 CA。密码及配置位置见 [配置说明](configuration.md)。

## 表与 API 权限

文章、Tag、SubTag 的主键直接使用笔记属性中的 `Id`（固定 UUID）；改名、移动或修改 Slug 时保留原 Id。

分类属性使用列表，兼容短链接、完整路径和显示别名。`TagRefs` 只在 `Tags` 中查找，`SubTags` 只在 `SubTags` 中查找；找不到的引用忽略，不报错、不阻断文章同步。无有效分类时关联为空。

- `Articles`：发布文章的属性与 Markdown；Slug、SourcePath 唯一。
- `Tags`、`SubTags`：独立分类；通过 `ArticleTags`、`ArticleSubTags` 分别与文章多对多关联。
- `SyncRuns`：按 Repository + CommitSha 唯一，记录入库及缓存刷新结果。

Supabase Data API 的 Exposed schemas 需加入对应 schema，读取请求通过 `Accept-Profile` 指定。`anon` 仅可查询五张内容表，不能修改，也不能读取 `SyncRuns` 和 `__EFMigrationsHistory`；授权与 RLS 由迁移配置。

## 迁移与验证

在项目根目录执行：

```powershell
dotnet ef migrations add <名称> --project .\Notion.Sync.Api -- --environment Development
dotnet ef migrations script --idempotent --project .\Notion.Sync.Api --output <SQL路径> -- --environment Development
dotnet ef database update --project .\Notion.Sync.Api -- --environment Development
dotnet ef migrations has-pending-model-changes --project .\Notion.Sync.Api -- --environment Development
```

开发固定使用 `blog_test`。生产需显式指定 `--environment Production`，执行身份需能读取迁移 Secret。API 不自动迁移；本次迁移不应用到旧 `public` schema。

`Notion.Sync.Api/Database/VerifyDevelopment.sql` 在管理员连接、`Search Path=blog_test` 下执行，验证关系、约束和权限，测试数据全部回滚。已验证内容表 GET 返回 200、写入被拒绝，内部表不可读。
