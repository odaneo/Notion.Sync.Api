# 配置

## 普通配置

| 文件 | 内容 |
| --- | --- |
| `Notion.Sync.Api/appsettings.json` | 连接模板、公开 CA、源仓库和分支 |
| `appsettings.Development.json` | `blog_test`、本地刷新地址 |
| `appsettings.Production.json` | `blog`、AWS Region/Secret 名称、生产刷新地址 |

开发不使用 AWS。地址、schema 和证书路径属于普通配置。

## 后端敏感值

本地只放在项目指定的 User Secrets（`C:\Users\...\secrets.json`），生产按下表保存：

| 统一配置键 | 用途 | AWS Secret |
| --- | --- | --- |
| `Supabase:username`、`Supabase:password` | 日常 writer 连接 | `notion-api-supabase` |
| `SupabaseMigration:username`、`SupabaseMigration:password` | 显式管理员迁移 | `notion-api-supabase-migration` |
| `Sync:Token` | Actions → .NET 认证 | `notion-api-obsidian-sync` |
| `Revalidation:Token` | .NET → Next.js 刷新认证 | `notion-api-revalidation` |

本地 JSON 分组为 `Supabase`、`SupabaseMigration`、`Sync`、`Revalidation`。AWS 两个数据库 Secret 均含 `username`、`password`，另两项只含 `Token`；加载前缀后两边配置键一致。Token 至少 32 字符，开发值已生成，生产使用独立值。

已有 `Kestrel:Certificates:Development:Password` 仅供本地 HTTPS，不需要上传 AWS。日常连接与迁移连接分别使用 `DbContext:ConnectionString`、`DbContext:MigrationConnectionString`。

本地同步脚本自动读取同一份 User Secrets，无需额外设置 `SYNC_TOKEN`；接口仍校验 Token。脚本默认执行同步、模拟刷新及结果校验，仅为本次进程设置临时回调地址，不改配置文件。

生产所需 Secret 已创建，两个数据库 Secret 的用户名与密码字段已配置。生产 `blog_writer` 已创建并验证登录；`blog` schema 尚待线上迁移。正常 API 不读取迁移 Secret。

## 笔记仓库 Actions

| GitHub 配置 | 值 |
| --- | --- |
| Actions Secret `SYNC_API_URL` | 完整 HTTPS 同步接口地址 |
| Actions Secret `SYNC_TOKEN` | 与生产 `Sync:Token` 相同的值 |

同步 Token 是允许在 GitHub Secrets 保存一份的例外；数据库密码不交给 Actions。工作流在笔记仓库 `.github/workflows/sync-blog.yml`，只发布 `main`，不使用 OIDC。

## 前端（下一阶段）

本地统一使用 `notion.sync.api.client/.env.local`；生产普通配置放 Worker vars，敏感值放 Worker Secrets。

| 变量 | 用途 |
| --- | --- |
| `HOME_URL` | 站点地址，构建环境也需提供 |
| `SUPABASE_URL` | Supabase HTTP API 地址 |
| `SUPABASE_SCHEMA` | 开发 `blog_test`，生产 `blog` |
| `SUPABASE_PUBLISHABLE_KEY` | API 读取凭据，权限由授权与 RLS 控制 |
| `REVALIDATE_TOKEN` | 与同环境的 .NET `Revalidation:Token` 配对 |

刷新接口需接受 Bearer Token，完成 Next.js 和相关 Cloudflare 缓存失效后返回 `{"revalidated":true}`；仅 HTTP 200 而没有确认内容不算成功。

运行与测试见 [README](../README.md)，迁移命令见 [数据库说明](database.md)。
