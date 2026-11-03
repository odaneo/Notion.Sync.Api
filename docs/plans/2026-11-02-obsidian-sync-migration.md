# Obsidian 博客迁移计划

分支：`codex/obsidian-sync-assessment`。本轮完成后端同步与笔记仓库 Actions，前端下一阶段实施，尚未部署生产。

## 同步规则

- 源仓库为 `odaneo/obsidian-blog-content` 的 `main`；Actions 打包当前检出的完整快照，Git SHA 用于记录版本。
- 仅布尔值 `Published: true` 的文章进入内容包；false、缺失字段和私人笔记正文不上传。取消发布或删除后，旧发布记录及关联移除。
- 文章、Tag、SubTag 直接使用笔记 `Id`（固定 UUID）；现已补齐 33 篇文章、9 个 Tag、31 个 SubTag，年度任务不处理。
- `TagRefs` 在 `Tags`、`SubTags` 在 `SubTags` 中解析短链接、路径及显示别名；无法唯一定位的引用跳过，不报错。分类清单包含分类元数据，不上传分类正文。
- 完整清单校验成功后，在一个事务内按 UUID 比较：新增、Hash 不同则更新、相同则跳过、清单外则删除。分类元数据分别比较。
- SHA-256 覆盖入库属性、Markdown、源路径、Git 编辑时间及排序后的分类 UUID；语法或别名变化不改变分类身份。
- Actions 使用原生队列串行执行，不取消正在运行的同步。后端使用 PostgreSQL 事务级锁及 `WorkflowRunNumber` 拒绝过时请求，同一提交重试仍走相同差异流程。
- `SyncRuns` 记录入库结果与 `RequiresRevalidation`。有变化或存在待刷新的记录时，入库后调用固定 Next.js 地址；失败返回 503，Actions 使用同一快照重试。
- 只有缓存接口明确返回 `{"revalidated":true}` 才确认刷新完成；较早回调不会清除较新版本的待刷新状态。无变化且无待刷新任务时直接结束。
- HTTPS + Bearer Token 认证；同步与刷新使用不同 Token。请求限制 10 MiB、固定仓库和分支，接口带限流；密钥不进入日志或镜像。具体位置见 [配置说明](../configuration.md)。

## 已完成

- [x] 沿用 Models、AppDbContext 和原有 EF Migrations，建立独立多对多分类关系与权限。
- [x] `blog_test` 已应用十个迁移；新增的运行序号及待刷新标记属于同步阶段迁移。生产尚未迁移。
- [x] `POST /api/sync/obsidian`、Hash 差异写入、事务回滚、无分类文章、唯一字段交换、并发及刷新重试。
- [x] 笔记仓库打包脚本、无 OIDC 的同步工作流、固定内容重试与发布内容过滤。
- [x] 删除旧 Notion/Hangfire/KV 后端和 Node helper，镜像改为单 .NET 进程，移除旧密钥 build args。
- [x] 本地密钥仍在指定 User Secrets，开发不依赖 AWS；AWS Compose 只向代理网络暴露 7031。
- [x] 后端单元及真实测试库验证、Actions 脚本测试；样例数据最终清理。
- [x] 本地一条命令同步未提交笔记，自动读取 Token；已将 18 篇发布文章导入 `blog_test`，模拟回调失败、重试及无变化跳过通过。真实前端刷新尚未接入。

## 前端下一阶段

- [ ] 服务端通过 Supabase REST API 读取对应 schema，移除 mock/KV 回退。
- [ ] Markdown 渲染替换 Notion recordMap，支持现有代码块、表格、Mermaid 和已发布文章链接；保持主要页面布局。
- [ ] 无分类文章仍正常渲染，首页、列表、正文、metadata、sitemap 共用长期缓存读取入口。
- [ ] 实现认证的 `POST /api/revalidate`，同步失效 Next.js 与实际存在的 Cloudflare 内容缓存。
- [ ] 移除 4 小时/300 秒过期、动态页面绕过和旧定时重验证；仅同步链路主动失效。OpenNext 缓存配置在 Worker 预览验证。
- [ ] 清理旧前端 Notion 依赖、KV binding 与已废弃文档。

## 联调与上线

- [ ] 创建生产 `blog` / `blog_writer`，配置生产密码、两个 Token、GitHub Secrets（`SYNC_API_URL`、`SYNC_TOKEN`）和刷新地址；正常 API 身份不读取管理员 Secret。
- [ ] 实际验证新增、修改、取消发布、删除、分类缺失、重复/并发请求及刷新失败后的恢复。
- [ ] 完成前端后再部署生产；核对归属后清理旧生产表、旧定时任务及旧 KV 资源。
