# 配置

## 普通配置

| 文件 | 内容 |
| --- | --- |
| `Notion.Sync.Api/appsettings.json` | 共用连接模板、迁移用户名、公开 CA 路径 |
| `Notion.Sync.Api/appsettings.Development.json` | 测试 schema、测试用户名 |
| `Notion.Sync.Api/appsettings.Production.json` | 生产 schema、生产用户名、AWS Region 和 Secret 名称 |

开发环境不需要 AWS。用户名、地址、schema、证书路径等普通配置不放入秘密文件，也不在多处重复声明。

## 后端密码

本地只放在 `C:\Users\...\secrets.json`：

```json
{
  "Supabase": { "password": "日常账号密码" },
  "SupabaseMigration": { "password": "postgres 管理员密码" }
}
```

已有的 `Kestrel:Certificates:Development:Password` 继续保留，供本地 HTTPS 使用。

| 统一读取的配置键 | 用途 | 生产 AWS Secrets Manager |
| --- | --- | --- |
| `Supabase:password` | API 日常连接，长期使用 | `notion-api-supabase` |
| `SupabaseMigration:password` | 仅显式数据库迁移时使用 | `notion-api-supabase-migration` |

AWS 两个 Secret 的目标内容均为 `{"password":"对应密码"}`，加载时分别加 `Supabase`、`SupabaseMigration` 前缀，与本地得到相同配置键。日常连接使用 `DbContext:ConnectionString`，迁移使用 `DbContext:MigrationConnectionString`。

迁移 Secret 已由用户创建，尚未读取验证。现有线上运行 Secret 先保持旧版所需字段，切换新版本时改用 `blog_writer` 密码并移除重复用户名。账号及迁移命令见 [数据库说明](database.md)。

## 前端配置

本地统一放在 `notion.sync.api.client/.env.local`，不再维护 `.env` 或 `.dev.vars`。生产普通配置放 Worker vars，敏感值放 Worker Secrets，变量名保持一致。

| 变量 | 用途 |
| --- | --- |
| `HOME_URL` | 站点地址；构建环境也需提供 |
| `SUPABASE_URL` | Supabase HTTP API 地址 |
| `SUPABASE_SCHEMA` | 开发 `blog_test`，生产 `blog` |
| `SUPABASE_PUBLISHABLE_KEY` | API 请求的 `apikey`；只读范围由表授权与 RLS 决定 |

前端目前尚未接入 Supabase；接入后以上配置长期使用，缓存重新取数时需要 API key。.NET 直接连接数据库不使用 API key。

同步阶段再配置 `Sync:Token`、`Revalidation:Token`、`Revalidation:Url` 和前端 `REVALIDATE_TOKEN`。token 遵循上述秘密保存位置，开发与生产使用不同值；详见 [迁移计划](plans/2026-11-02-obsidian-sync-migration.md)。
