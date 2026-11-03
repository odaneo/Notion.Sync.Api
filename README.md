# Obsidian 博客同步

```text
Obsidian → GitHub Actions → .NET → Supabase
                              ↓ 入库后
                       Next.js / Cloudflare 刷新
```

后端与笔记仓库 Actions 已实现；前端的 Supabase 读取、Markdown 渲染和刷新接口留到下一阶段。当前分支不用于生产部署。

- 每次发送完整发布快照，按 UUID 和 SHA-256 比较，只新增、更新、删除有差异的记录。
- 仅同步布尔值 `Published: true`；草稿正文不进入内容包。
- 分类支持短链接、完整路径、显示别名；找不到就忽略，文章可没有分类。
- Actions 串行、数据库事务锁和运行序号防止旧请求覆盖；刷新失败可重试，不重复写业务数据。
- 同步接口为 `POST /api/sync/obsidian`，使用 HTTPS 和 Bearer Token；同步工作流不使用 OIDC。

```powershell
node .\scripts\sync-local.cjs
dotnet run --project .\Notion.Sync.Api --launch-profile http
dotnet test .\tests\Notion.Sync.Api.Tests
docker compose up --build
```

开发连接 Supabase `blog_test`，本地 Docker 只运行 .NET 并只读挂载指定 User Secrets。生产使用 `blog` 和 AWS Secrets Manager，迁移单独执行。

本地同步命令会临时启动 .NET，读取 Obsidian 工作区（包括未提交修改），自动从 User Secrets 取 Token，写入 `blog_test` 后校验重复快照跳过；需要刷新时模拟一次回调失败并验证重试，完成后退出。刷新使用本地模拟服务。默认笔记目录按当前用户主目录 + `Documents/Obsidian` 拼接，可用 `--vault <路径>` 指定；运行时会打印实际路径。GitHub Actions 仍只处理已提交内容。

自定义笔记目录：`node .\scripts\sync-local.cjs --vault "D:\Notes\Obsidian"`。

- [配置及上线前待填项](docs/configuration.md)
- [数据库与迁移](docs/database.md)
- [实施状态与下一阶段](docs/plans/2026-11-02-obsidian-sync-migration.md)
