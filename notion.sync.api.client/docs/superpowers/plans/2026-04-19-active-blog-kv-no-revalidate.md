# 主动博客 KV 缓存无 Revalidate 入口实施计划

> **给 agentic workers:** REQUIRED SUB-SKILL: 使用 superpowers:subagent-driven-development 或 superpowers:executing-plans 按任务逐项执行。步骤使用 checkbox 语法跟踪。

**目标:** 将博客前台读取链路从 Supabase RPC 改为 Cloudflare KV 主动缓存，不暴露 revalidate 入口，由 Next.js 短 TTL 页面缓存自然刷新。

**架构:** AWS 上的 .NET Hangfire 同步任务继续负责 Notion 到 Supabase 的同步，并在同步成功后直接调用 Cloudflare KV REST API 发布前台 JSON。Next.js 部署在 Cloudflare 上，只通过 `BLOG_CACHE` binding 读取 KV；HTML/ISR 缓存保留，但页面 `revalidate` 从 3600 秒缩短到 120 到 300 秒，避免任何内部刷新 HTTP 入口。

**技术栈:** Next.js 15 App Router、React 19、TypeScript、OpenNext Cloudflare、Cloudflare Workers KV、Cloudflare R2 incremental cache、Wrangler、.NET 8、Hangfire、Entity Framework Core、HttpClient、pnpm。

---

## 范围和约束

- 不新增 Redis、独立 Worker、独立队列或额外部署单元。
- 不创建 `/api/revalidate`，也不保留 `CACHE_REVALIDATE_SECRET`。
- 不让 Next.js 前台请求访问 Supabase。
- 不手动写 OpenNext 的 R2 incremental cache 内部对象。
- 不改 UI 样式和现有中文文案。
- 不改 Notion 渲染组件。
- 客户端改动位于 `notion.sync.api.client`。
- 同步服务改动位于同级 `../Notion.Sync.Api`，当前已确认是 `net8.0` Hangfire 服务。

## 最终数据流

```text
Hangfire 每 30 分钟执行
  -> 读取 Notion 标签和文章列表
  -> 写入 Supabase
  -> 调用当前 Node 同步服务更新文章内容
  -> 从 Supabase 读取已发布文章快照
  -> 写 Cloudflare KV 版本化 key
  -> 最后写 blog:active

用户访问 Cloudflare 上的 Next.js
  -> 命中页面 HTML 缓存则直接返回
  -> 页面缓存自然过期后重新渲染
  -> 渲染过程只读 Cloudflare KV
  -> 不访问 Supabase
```

## KV Key 设计

每次同步生成一个不可变版本，最后更新 active 指针。

```text
blog:active = 2026-04-19T10-30-00Z
blog:v:2026-04-19T10-30-00Z:manifest
blog:v:2026-04-19T10-30-00Z:home
blog:v:2026-04-19T10-30-00Z:articles
blog:v:2026-04-19T10-30-00Z:tags
blog:v:2026-04-19T10-30-00Z:tag:<tagSlug>
blog:v:2026-04-19T10-30-00Z:article:<articleSlug>
```

版本化 key 写入时加 `expiration_ttl=604800`，保留 7 天。`blog:active` 不设置过期时间。

## JSON Schema

Cloudflare KV 里存 camelCase JSON，和当前 `src/type/api.type.ts` 对齐。

### `blog:v:<version>:home`

```json
{
  "tags": [
    {
      "id": "tag-id",
      "slug": "nextjs",
      "title": "Next.js",
      "articleCount": 12,
      "description": "Next.js 相关文章",
      "lucideIconName": "box"
    }
  ],
  "recommendArticles": [
    {
      "id": "article-id",
      "title": "文章标题",
      "slug": "article-slug",
      "lastEditedTime": "2026-04-19T10:30:00Z",
      "tags": [],
      "subTags": []
    }
  ]
}
```

### `blog:v:<version>:articles`

```json
[
  {
    "id": "article-id",
    "title": "文章标题",
    "slug": "article-slug",
    "lastEditedTime": "2026-04-19T10:30:00Z",
    "tags": [],
    "subTags": []
  }
]
```

### `blog:v:<version>:tags`

```json
[
  {
    "slug": "nextjs",
    "title": "Next.js",
    "articles": []
  }
]
```

### `blog:v:<version>:tag:<tagSlug>`

```json
{
  "tag": {
    "id": "tag-id",
    "slug": "nextjs",
    "title": "Next.js",
    "articleCount": 12,
    "description": "Next.js 相关文章",
    "lucideIconName": "box"
  },
  "articles": []
}
```

### `blog:v:<version>:article:<articleSlug>`

```json
{
  "content": "{\"block\":{}}",
  "title": "文章标题",
  "slug": "article-slug",
  "lastEditedTime": "2026-04-19T10:30:00Z",
  "id": "article-id",
  "description": "文章描述",
  "tags": [],
  "subTags": []
}
```

### `blog:v:<version>:manifest`

```json
{
  "version": "2026-04-19T10-30-00Z",
  "generatedAt": "2026-04-19T10:30:00Z",
  "articleCount": 42,
  "tagCount": 8,
  "keys": [
    "blog:v:2026-04-19T10-30-00Z:home",
    "blog:v:2026-04-19T10-30-00Z:articles",
    "blog:v:2026-04-19T10-30-00Z:tags"
  ]
}
```

## Cloudflare KV REST API 对接

同步服务使用 API Token 调 Cloudflare API。API Token 需要目标 account 下的 `Workers KV Storage Write` 权限。

### 配置项

生产环境通过 AWS Secrets Manager 注入到 `CloudflareKv:*` 配置前缀:

```json
{
  "accountId": "cloudflare-account-id",
  "namespaceId": "cloudflare-kv-namespace-id",
  "apiToken": "cloudflare-api-token",
  "versionedKeyExpirationTtlSeconds": 604800
}
```

本地开发使用 .NET user-secrets:

```bash
dotnet user-secrets set "CloudflareKv:AccountId" "cloudflare-account-id"
dotnet user-secrets set "CloudflareKv:NamespaceId" "cloudflare-kv-namespace-id"
dotnet user-secrets set "CloudflareKv:ApiToken" "cloudflare-api-token"
dotnet user-secrets set "CloudflareKv:VersionedKeyExpirationTtlSeconds" "604800"
```

### 写版本化 key

```http
PUT https://api.cloudflare.com/client/v4/accounts/{accountId}/storage/kv/namespaces/{namespaceId}/values/{urlEncodedKey}?expiration_ttl=604800
Authorization: Bearer {apiToken}
Content-Type: application/json

{...json value...}
```

成功响应:

```json
{
  "success": true,
  "errors": [],
  "messages": [],
  "result": {}
}
```

### 写 active 指针

```http
PUT https://api.cloudflare.com/client/v4/accounts/{accountId}/storage/kv/namespaces/{namespaceId}/values/blog%3Aactive
Authorization: Bearer {apiToken}
Content-Type: text/plain

2026-04-19T10-30-00Z
```

发布顺序:

```text
1. 写所有 blog:v:<version>:* key，全部带 expiration_ttl=604800
2. 所有版本化 key 成功后，写 blog:active
3. blog:active 写失败时，前台继续读旧版本
4. blog:active 写成功后，新版本开始被前台自然读取
```

## 文件结构计划

客户端:

- 修改 `wrangler.jsonc`: 增加 `BLOG_CACHE` KV namespace binding。
- 重新生成 `cloudflare-env.d.ts`: 让 `Cloudflare.Env` 包含 `BLOG_CACHE`。
- 创建 `src/type/blog-cache.type.ts`: 定义 manifest 和标签详情类型。
- 创建 `src/utils/blog-cache/server.ts`: 读取 `blog:active` 和版本化 key。
- 修改 `src/app/page.tsx`: 改读 KV，设置 `revalidate = 120`。
- 修改 `src/app/blog/page.tsx`: 改读 KV，设置 `revalidate = 120`。
- 修改 `src/app/tag/page.tsx`: 改读 KV，设置 `revalidate = 120`。
- 修改 `src/app/tag/[tag]/page.tsx`: 改读 KV，设置 `revalidate = 120`。
- 修改 `src/app/blog/[tag]/[slug]/page.tsx`: 改读 KV，设置 `revalidate = 300`。
- 修改 `src/app/server-sitemap.xml/route.ts`: 改读 KV，响应缓存改为 300 秒。
- 修改 `.dev.vars.example`: 移除 Supabase 示例变量，保留 `HOME_URL`。
- 修改 `package.json` 和 `pnpm-lock.yaml`: 移除 `@supabase/supabase-js` 和 `@supabase/ssr`。
- 删除 `src/utils/supabase/server.ts`: 前台不再有 Supabase 读取层。

同步服务:

- 修改 `../Notion.Sync.Api/appsettings.json`: 增加 `AWS:SecretNameCloudflareKv`。
- 修改 `../Notion.Sync.Api/Program.cs`: 从 Secrets Manager 加载 Cloudflare KV 配置，注册 KV client 和发布服务。
- 创建 `../Notion.Sync.Api/Options/CloudflareKvOptions.cs`: Cloudflare KV 配置模型。
- 创建 `../Notion.Sync.Api/Dtos/BlogCacheDtos.cs`: 与前台 TypeScript 类型对齐的缓存 DTO。
- 创建 `../Notion.Sync.Api/Business/IServices/ICloudflareKvClient.cs`: 定义 KV 写入接口。
- 创建 `../Notion.Sync.Api/Business/Services/CloudflareKvClient.cs`: 封装 Cloudflare KV REST API。
- 创建 `../Notion.Sync.Api/Business/IServices/IBlogCachePublisher.cs`: 定义发布接口。
- 创建 `../Notion.Sync.Api/Business/Services/BlogCachePublisher.cs`: 从 `AppDbContext` 生成快照并发布 KV。
- 修改 `../Notion.Sync.Api/Job/NotionDatabaseSyncJobService.cs`: 在 Supabase 和 Node 内容同步成功后发布 KV。

## 任务 1: 添加客户端 KV binding

**文件:**
- 修改: `wrangler.jsonc`
- 修改: `.dev.vars.example`
- 生成: `cloudflare-env.d.ts`

- [ ] **步骤 1: 创建 production KV namespace**

运行:

```bash
pnpm exec wrangler kv namespace create BLOG_CACHE
```

期望输出包含:

```text
id = "<production namespace id>"
```

- [ ] **步骤 2: 创建 preview KV namespace**

运行:

```bash
pnpm exec wrangler kv namespace create BLOG_CACHE --preview
```

期望输出包含:

```text
preview_id = "<preview namespace id>"
```

- [ ] **步骤 3: 修改 Wrangler 配置**

在 `wrangler.jsonc` 顶层加入 `kv_namespaces`，保留现有 `services`、`r2_buckets`、`durable_objects` 和 `migrations`:

```jsonc
"kv_namespaces": [
  {
    "binding": "BLOG_CACHE",
    "id": "<production namespace id>",
    "preview_id": "<preview namespace id>"
  }
]
```

- [ ] **步骤 4: 更新本地环境示例**

将 `.dev.vars.example` 改为:

```text
HOME_URL=http://localhost:3000
```

- [ ] **步骤 5: 重新生成 Cloudflare 类型**

运行:

```bash
pnpm run cf:typegen
```

期望 `cloudflare-env.d.ts` 的 `Cloudflare.Env` 至少包含:

```ts
interface Env {
  ASSETS: Fetcher;
  BLOG_CACHE: KVNamespace;
  HOME_URL: string;
}
```

- [ ] **步骤 6: 验证客户端类型**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 7: 提交客户端 binding 变更**

运行:

```bash
git add wrangler.jsonc .dev.vars.example cloudflare-env.d.ts
git commit -m "chore: add blog cache kv binding"
```

## 任务 2: 添加客户端 KV 读取层

**文件:**
- 创建: `src/type/blog-cache.type.ts`
- 创建: `src/utils/blog-cache/server.ts`

- [ ] **步骤 1: 创建缓存类型文件**

创建 `src/type/blog-cache.type.ts`:

```ts
import {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  GetTagsAndRecommendArticlesResponseType,
  GetTagsWithArticlesResponseType,
  TagsType,
} from "@/type/api.type";

export type TagDetailWithArticlesType = {
  tag: TagsType;
  articles: ArticlesType[];
};

export type BlogCacheManifestType = {
  version: string;
  generatedAt: string;
  articleCount: number;
  tagCount: number;
  keys: string[];
};

export type BlogCachePayloadType = {
  home: GetTagsAndRecommendArticlesResponseType;
  articles: ArticlesType[];
  tags: GetTagsWithArticlesResponseType[];
  tagDetails: Record<string, TagDetailWithArticlesType>;
  articleDetails: Record<string, GetArticleWithSubTagsResponseType>;
};
```

- [ ] **步骤 2: 创建 KV 读取实现**

创建 `src/utils/blog-cache/server.ts`:

```ts
import "server-only";
import { getCloudflareContext } from "@opennextjs/cloudflare";
import {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  GetTagsAndRecommendArticlesResponseType,
  GetTagsWithArticlesResponseType,
} from "@/type/api.type";
import {
  BlogCacheManifestType,
  TagDetailWithArticlesType,
} from "@/type/blog-cache.type";

const ACTIVE_VERSION_KEY = "blog:active";
const ACTIVE_VERSION_CACHE_TTL = 30;
const VERSIONED_DATA_CACHE_TTL = 300;

async function getBlogCache() {
  const { env } = await getCloudflareContext({ async: true });
  return env.BLOG_CACHE;
}

function getVersionedKey(version: string, name: string) {
  return `blog:v:${version}:${name}`;
}

async function getActiveVersion() {
  const cache = await getBlogCache();
  return cache.get(ACTIVE_VERSION_KEY, {
    cacheTtl: ACTIVE_VERSION_CACHE_TTL,
  });
}

async function getVersionedJson<T>(name: string) {
  const version = await getActiveVersion();

  if (!version) {
    return null;
  }

  const cache = await getBlogCache();
  const value = await cache.get(getVersionedKey(version, name), {
    cacheTtl: VERSIONED_DATA_CACHE_TTL,
  });

  return value ? (JSON.parse(value) as T) : null;
}

export async function getBlogCacheManifest() {
  return getVersionedJson<BlogCacheManifestType>("manifest");
}

export async function getTagsAndRecommendArticles() {
  return getVersionedJson<GetTagsAndRecommendArticlesResponseType>("home");
}

export async function getAllArticles() {
  return getVersionedJson<ArticlesType[]>("articles");
}

export async function getTagsWithArticles() {
  return getVersionedJson<GetTagsWithArticlesResponseType[]>("tags");
}

export async function getArticleWithSubTags(slug: string) {
  if (!slug) {
    return null;
  }

  return getVersionedJson<GetArticleWithSubTagsResponseType>(
    `article:${slug}`,
  );
}

export async function getTagDetailWithArticles(tag: string) {
  if (!tag) {
    return null;
  }

  return getVersionedJson<TagDetailWithArticlesType>(`tag:${tag}`);
}
```

- [ ] **步骤 3: 验证类型**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 4: 提交缓存读取层**

运行:

```bash
git add src/type/blog-cache.type.ts src/utils/blog-cache/server.ts
git commit -m "feat: read blog data from cloudflare kv"
```

## 任务 3: 切换客户端页面并设置短 TTL

**文件:**
- 修改: `src/app/page.tsx`
- 修改: `src/app/blog/page.tsx`
- 修改: `src/app/tag/page.tsx`
- 修改: `src/app/tag/[tag]/page.tsx`
- 修改: `src/app/blog/[tag]/[slug]/page.tsx`
- 修改: `src/app/server-sitemap.xml/route.ts`

- [ ] **步骤 1: 修改首页**

将 `src/app/page.tsx` 导入改为:

```ts
import { getTagsAndRecommendArticles } from "@/utils/blog-cache/server";
```

在导入后加入:

```ts
export const revalidate = 120;
```

- [ ] **步骤 2: 修改文章列表页**

将 `src/app/blog/page.tsx` 导入改为:

```ts
import { getAllArticles } from "@/utils/blog-cache/server";
```

在导入后加入:

```ts
export const revalidate = 120;
```

- [ ] **步骤 3: 修改标签总览页**

将 `src/app/tag/page.tsx` 导入改为:

```ts
import { getTagsWithArticles } from "@/utils/blog-cache/server";
```

在导入后加入:

```ts
export const revalidate = 120;
```

- [ ] **步骤 4: 修改标签详情页**

将 `src/app/tag/[tag]/page.tsx` 导入改为:

```ts
import { getTagDetailWithArticles } from "@/utils/blog-cache/server";
```

在导入后加入:

```ts
export const revalidate = 120;
```

- [ ] **步骤 5: 修改文章详情页**

将 `src/app/blog/[tag]/[slug]/page.tsx` 导入改为:

```ts
import { getArticleWithSubTags } from "@/utils/blog-cache/server";
```

在导入后加入:

```ts
export const revalidate = 300;
```

- [ ] **步骤 6: 修改 sitemap route**

将 `src/app/server-sitemap.xml/route.ts` 改为:

```ts
import { getServerSideSitemap } from "next-sitemap";
import { getTagsWithArticles } from "@/utils/blog-cache/server";
import { Changefreq } from "@/type/sitemap.type";

export async function GET() {
  const tagsData = await getTagsWithArticles();
  const tags = Array.isArray(tagsData) ? tagsData : [];

  const now = new Date().toISOString();

  const fields = [
    {
      loc: `${process.env.HOME_URL}`,
      lastmod: now,
      changefreq: "daily" as Changefreq,
      priority: 1.0,
    },
    {
      loc: `${process.env.HOME_URL}/blog`,
      lastmod: now,
      changefreq: "daily" as Changefreq,
      priority: 0.8,
    },
    {
      loc: `${process.env.HOME_URL}/tag`,
      lastmod: now,
      changefreq: "weekly" as Changefreq,
      priority: 0.8,
    },
    {
      loc: `${process.env.HOME_URL}/contact`,
      lastmod: now,
      changefreq: "monthly" as Changefreq,
      priority: 0.5,
    },
  ];

  tags.forEach((tag) => {
    fields.push({
      loc: `${process.env.HOME_URL}/tag/${encodeURIComponent(tag.slug)}`,
      lastmod: now,
      changefreq: "weekly",
      priority: 0.7,
    });

    tag.articles?.forEach((article) => {
      fields.push({
        loc: `${process.env.HOME_URL}/blog/${encodeURIComponent(tag.slug)}/${encodeURIComponent(article.slug)}`,
        lastmod: article.lastEditedTime
          ? new Date(article.lastEditedTime).toISOString()
          : now,
        changefreq: "weekly",
        priority: 0.9,
      });
    });
  });

  const res = await getServerSideSitemap(fields);
  res.headers.set(
    "Cache-Control",
    "public, max-age=300, s-maxage=300, stale-while-revalidate=60",
  );
  return res;
}
```

- [ ] **步骤 7: 确认没有 revalidate route**

运行:

```bash
find src/app/api -maxdepth 3 -type f
```

期望:

```text
不存在 src/app/api/revalidate/route.ts
```

- [ ] **步骤 8: 验证类型**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 9: 提交页面切换**

运行:

```bash
git add src/app/page.tsx src/app/blog/page.tsx src/app/tag/page.tsx src/app/tag/[tag]/page.tsx src/app/blog/[tag]/[slug]/page.tsx src/app/server-sitemap.xml/route.ts
git commit -m "feat: serve blog pages from kv cache"
```

## 任务 4: 移除客户端 Supabase 读取

**文件:**
- 删除: `src/utils/supabase/server.ts`
- 修改: `package.json`
- 修改: `pnpm-lock.yaml`
- 修改: `.dev.vars.example`
- 生成: `cloudflare-env.d.ts`

- [ ] **步骤 1: 确认 Supabase 工具不再被引用**

运行:

```bash
rg "@/utils/supabase/server|createClient|SUPABASE_URL|SUPABASE_SECRET_KEY" src
```

期望:

```text
无输出
```

- [ ] **步骤 2: 删除旧 Supabase 工具文件**

运行:

```bash
rm src/utils/supabase/server.ts
```

- [ ] **步骤 3: 移除 Supabase 前台依赖**

运行:

```bash
pnpm remove @supabase/ssr @supabase/supabase-js
```

期望:

```text
dependencies:
- @supabase/ssr
- @supabase/supabase-js
```

- [ ] **步骤 4: 重新生成 Cloudflare 类型**

运行:

```bash
pnpm run cf:typegen
```

- [ ] **步骤 5: 验证没有 Supabase 残留**

运行:

```bash
rg "supabase|SUPABASE|@supabase" src package.json cloudflare-env.d.ts .dev.vars.example
```

期望:

```text
无输出
```

- [ ] **步骤 6: 验证客户端**

运行:

```bash
pnpm exec tsc --noEmit
pnpm run lint
pnpm cf:build
```

期望:

```text
无 TypeScript error
lint 通过
OpenNext Cloudflare build 成功
```

- [ ] **步骤 7: 提交 Supabase 移除**

运行:

```bash
git add package.json pnpm-lock.yaml .dev.vars.example cloudflare-env.d.ts src/utils/supabase/server.ts
git commit -m "chore: remove frontend supabase reads"
```

## 任务 5: 为 .NET 同步服务添加 Cloudflare KV 配置

**文件:**
- 修改: `../Notion.Sync.Api/appsettings.json`
- 创建: `../Notion.Sync.Api/Options/CloudflareKvOptions.cs`
- 修改: `../Notion.Sync.Api/Program.cs`

- [ ] **步骤 1: 增加 Secrets Manager 名称**

修改 `../Notion.Sync.Api/appsettings.json` 的 `AWS` 节:

```json
{
  "AWS": {
    "Region": "ap-northeast-1",
    "SecretNameRDS": "rds!db-288200fe-c6cc-460c-be31-c373b4609c18",
    "SecretNameNotionToken": "notion-api-token",
    "SecretNameHangfireUser": "notion-api-hangfire-user",
    "SecretNameSupabase": "notion-api-supabase",
    "SecretNameCloudflareKv": "notion-api-cloudflare-kv"
  }
}
```

- [ ] **步骤 2: 创建配置模型**

创建 `../Notion.Sync.Api/Options/CloudflareKvOptions.cs`:

```csharp
namespace Notion.Sync.Api.Options
{
    public class CloudflareKvOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string NamespaceId { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public int VersionedKeyExpirationTtlSeconds { get; set; } = 604800;
    }
}
```

- [ ] **步骤 3: 从 Secrets Manager 加载配置**

在 `../Notion.Sync.Api/Program.cs` 顶部加入:

```csharp
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Business.Services;
using Notion.Sync.Api.Options;
```

在生产环境 Secrets Manager 加载段落中追加:

```csharp
.AddSecretsManager(
    builder.Configuration["AWS:SecretNameCloudflareKv"]!,
    configurationKeyPrefix: "CloudflareKv"
);
```

- [ ] **步骤 4: 注册配置和服务**

在 `builder.Services.AddHttpClient();` 后加入:

```csharp
builder.Services.Configure<CloudflareKvOptions>(
    builder.Configuration.GetSection("CloudflareKv")
);
builder.Services.AddHttpClient<ICloudflareKvClient, CloudflareKvClient>();
builder.Services.AddScoped<IBlogCachePublisher, BlogCachePublisher>();
```

- [ ] **步骤 5: 本地写入 user-secrets**

在 `../Notion.Sync.Api` 目录运行:

```bash
dotnet user-secrets set "CloudflareKv:AccountId" "<cloudflare-account-id>"
dotnet user-secrets set "CloudflareKv:NamespaceId" "<cloudflare-kv-namespace-id>"
dotnet user-secrets set "CloudflareKv:ApiToken" "<cloudflare-api-token>"
dotnet user-secrets set "CloudflareKv:VersionedKeyExpirationTtlSeconds" "604800"
```

- [ ] **步骤 6: 验证同步服务编译**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 7: 提交配置变更**

运行:

```bash
git add ../Notion.Sync.Api/appsettings.json ../Notion.Sync.Api/Options/CloudflareKvOptions.cs ../Notion.Sync.Api/Program.cs
git commit -m "chore: configure cloudflare kv publishing"
```

## 任务 6: 添加 .NET KV 发布 DTO

**文件:**
- 创建: `../Notion.Sync.Api/Dtos/BlogCacheDtos.cs`

- [ ] **步骤 1: 创建 DTO 文件**

创建 `../Notion.Sync.Api/Dtos/BlogCacheDtos.cs`:

```csharp
namespace Notion.Sync.Api.Dtos
{
    public record BlogCacheTagDto(
        string Id,
        string Slug,
        string Title,
        int ArticleCount,
        string Description,
        string LucideIconName
    );

    public record BlogCacheSubTagDto(string Id, string Title, string Slug);

    public record BlogCacheArticleSummaryDto(
        string Id,
        string Title,
        string Slug,
        DateTime LastEditedTime,
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheSubTagDto> SubTags
    );

    public record BlogCacheArticleDetailDto(
        string Content,
        string Title,
        string Slug,
        DateTime LastEditedTime,
        string Id,
        string Description,
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheSubTagDto> SubTags
    );

    public record BlogCacheHomeDto(
        IReadOnlyList<BlogCacheTagDto> Tags,
        IReadOnlyList<BlogCacheArticleSummaryDto> RecommendArticles
    );

    public record BlogCacheTagWithArticlesDto(
        string Slug,
        string Title,
        IReadOnlyList<BlogCacheArticleSummaryDto> Articles
    );

    public record BlogCacheTagDetailDto(
        BlogCacheTagDto Tag,
        IReadOnlyList<BlogCacheArticleSummaryDto> Articles
    );

    public record BlogCacheManifestDto(
        string Version,
        DateTime GeneratedAt,
        int ArticleCount,
        int TagCount,
        IReadOnlyList<string> Keys
    );
}
```

- [ ] **步骤 2: 验证 DTO 编译**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 3: 提交 DTO**

运行:

```bash
git add ../Notion.Sync.Api/Dtos/BlogCacheDtos.cs
git commit -m "feat: add blog cache payload dto"
```

## 任务 7: 添加 .NET Cloudflare KV REST client

**文件:**
- 创建: `../Notion.Sync.Api/Business/IServices/ICloudflareKvClient.cs`
- 创建: `../Notion.Sync.Api/Business/Services/CloudflareKvClient.cs`

- [ ] **步骤 1: 创建接口**

创建 `../Notion.Sync.Api/Business/IServices/ICloudflareKvClient.cs`:

```csharp
namespace Notion.Sync.Api.Business.IServices
{
    public interface ICloudflareKvClient
    {
        Task PutJsonAsync(string key, string json, int? expirationTtlSeconds, CancellationToken cancellationToken);
        Task PutTextAsync(string key, string value, CancellationToken cancellationToken);
    }
}
```

- [ ] **步骤 2: 创建 REST client**

创建 `../Notion.Sync.Api/Business/Services/CloudflareKvClient.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Options;

namespace Notion.Sync.Api.Business.Services
{
    public class CloudflareKvClient : ICloudflareKvClient
    {
        private readonly HttpClient _httpClient;
        private readonly CloudflareKvOptions _options;
        private readonly ILogger<CloudflareKvClient> _logger;

        public CloudflareKvClient(
            HttpClient httpClient,
            IOptions<CloudflareKvOptions> options,
            ILogger<CloudflareKvClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        }

        public Task PutJsonAsync(string key, string json, int? expirationTtlSeconds, CancellationToken cancellationToken)
        {
            return PutAsync(key, json, "application/json", expirationTtlSeconds, cancellationToken);
        }

        public Task PutTextAsync(string key, string value, CancellationToken cancellationToken)
        {
            return PutAsync(key, value, "text/plain", null, cancellationToken);
        }

        private async Task PutAsync(string key, string value, string contentType, int? expirationTtlSeconds, CancellationToken cancellationToken)
        {
            var url = BuildValueUrl(key, expirationTtlSeconds);

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                using var content = new StringContent(value, Encoding.UTF8, contentType);
                using var response = await _httpClient.PutAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                if (!ShouldRetry(response.StatusCode) || attempt == 3)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Cloudflare KV write failed for key {key}. Status: {(int)response.StatusCode}. Body: {body}"
                    );
                }

                _logger.LogWarning(
                    "Cloudflare KV write retry {Attempt} for key {Key}. Status: {StatusCode}",
                    attempt,
                    key,
                    response.StatusCode
                );

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        private string BuildValueUrl(string key, int? expirationTtlSeconds)
        {
            var encodedKey = Uri.EscapeDataString(key);
            var url =
                $"https://api.cloudflare.com/client/v4/accounts/{_options.AccountId}/storage/kv/namespaces/{_options.NamespaceId}/values/{encodedKey}";

            return expirationTtlSeconds.HasValue
                ? $"{url}?expiration_ttl={expirationTtlSeconds.Value}"
                : url;
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests
                || statusCode == HttpStatusCode.InternalServerError
                || statusCode == HttpStatusCode.BadGateway
                || statusCode == HttpStatusCode.ServiceUnavailable
                || statusCode == HttpStatusCode.GatewayTimeout;
        }
    }
}
```

- [ ] **步骤 3: 验证 REST client 编译**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 4: 提交 REST client**

运行:

```bash
git add ../Notion.Sync.Api/Business/IServices/ICloudflareKvClient.cs ../Notion.Sync.Api/Business/Services/CloudflareKvClient.cs
git commit -m "feat: add cloudflare kv client"
```

## 任务 8: 添加博客缓存发布服务

**文件:**
- 创建: `../Notion.Sync.Api/Business/IServices/IBlogCachePublisher.cs`
- 创建: `../Notion.Sync.Api/Business/Services/BlogCachePublisher.cs`

- [ ] **步骤 1: 创建发布接口**

创建 `../Notion.Sync.Api/Business/IServices/IBlogCachePublisher.cs`:

```csharp
namespace Notion.Sync.Api.Business.IServices
{
    public interface IBlogCachePublisher
    {
        Task PublishAsync(CancellationToken cancellationToken);
    }
}
```

- [ ] **步骤 2: 创建发布服务**

创建 `../Notion.Sync.Api/Business/Services/BlogCachePublisher.cs`:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notion.Sync.Api.Business.IServices;
using Notion.Sync.Api.Database;
using Notion.Sync.Api.Dtos;
using Notion.Sync.Api.Models;
using Notion.Sync.Api.Options;

namespace Notion.Sync.Api.Business.Services
{
    public class BlogCachePublisher : IBlogCachePublisher
    {
        private readonly AppDbContext _dbContext;
        private readonly ICloudflareKvClient _cloudflareKvClient;
        private readonly CloudflareKvOptions _options;
        private readonly ILogger<BlogCachePublisher> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BlogCachePublisher(
            AppDbContext dbContext,
            ICloudflareKvClient cloudflareKvClient,
            IOptions<CloudflareKvOptions> options,
            ILogger<BlogCachePublisher> logger)
        {
            _dbContext = dbContext;
            _cloudflareKvClient = cloudflareKvClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task PublishAsync(CancellationToken cancellationToken)
        {
            var version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
            var prefix = $"blog:v:{version}";

            var articles = await _dbContext.NotionArticles
                .AsNoTracking()
                .Where(article => article.Published)
                .Include(article => article.Article)
                .Include(article => article.NotionArticleTags)
                    .ThenInclude(articleTag => articleTag.Tag)
                .Include(article => article.NotionArticleSubTags)
                    .ThenInclude(articleSubTag => articleSubTag.SubTag)
                .OrderByDescending(article => article.LastEditedTime)
                .ToListAsync(cancellationToken);

            var tags = await _dbContext.Tags
                .AsNoTracking()
                .OrderBy(tag => tag.Title)
                .ToListAsync(cancellationToken);

            var tagDtos = tags
                .Select(tag => ToTagDto(tag, articles))
                .Where(tag => tag.ArticleCount > 0)
                .ToList();

            var articleSummaries = articles
                .Select(article => ToArticleSummaryDto(article, tagDtos))
                .ToList();

            var home = new BlogCacheHomeDto(
                tagDtos,
                articles
                    .Where(article => article.Recommend)
                    .Select(article => ToArticleSummaryDto(article, tagDtos))
                    .ToList()
            );

            var tagList = tagDtos
                .Select(tag => new BlogCacheTagWithArticlesDto(
                    tag.Slug,
                    tag.Title,
                    articleSummaries
                        .Where(article => article.Tags.Any(articleTag => articleTag.Id == tag.Id))
                        .ToList()
                ))
                .ToList();

            var keys = new List<string>();

            await PutJsonAsync($"{prefix}:home", home, keys, cancellationToken);
            await PutJsonAsync($"{prefix}:articles", articleSummaries, keys, cancellationToken);
            await PutJsonAsync($"{prefix}:tags", tagList, keys, cancellationToken);

            foreach (var tag in tagDtos)
            {
                var tagArticles = articleSummaries
                    .Where(article => article.Tags.Any(articleTag => articleTag.Id == tag.Id))
                    .ToList();
                var tagDetail = new BlogCacheTagDetailDto(tag, tagArticles);
                await PutJsonAsync($"{prefix}:tag:{tag.Slug}", tagDetail, keys, cancellationToken);
            }

            foreach (var article in articles)
            {
                var detail = ToArticleDetailDto(article, tagDtos);
                await PutJsonAsync($"{prefix}:article:{article.Slug}", detail, keys, cancellationToken);
            }

            var manifest = new BlogCacheManifestDto(
                version,
                DateTime.UtcNow,
                articles.Count,
                tagDtos.Count,
                keys
            );

            await PutJsonAsync($"{prefix}:manifest", manifest, keys, cancellationToken);
            await _cloudflareKvClient.PutTextAsync("blog:active", version, cancellationToken);

            _logger.LogInformation(
                "Published blog cache version {Version}. Articles: {ArticleCount}, Tags: {TagCount}",
                version,
                articles.Count,
                tagDtos.Count
            );
        }

        private async Task PutJsonAsync(
            string key,
            object value,
            List<string> keys,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _cloudflareKvClient.PutJsonAsync(
                key,
                json,
                _options.VersionedKeyExpirationTtlSeconds,
                cancellationToken
            );
            keys.Add(key);
        }

        private static BlogCacheTagDto ToTagDto(Tag tag, List<NotionArticle> publishedArticles)
        {
            var articleCount = publishedArticles.Count(article =>
                article.NotionArticleTags.Any(articleTag => articleTag.TagId == tag.Id)
            );

            return new BlogCacheTagDto(
                tag.Id,
                tag.Slug ?? string.Empty,
                tag.Title ?? string.Empty,
                articleCount,
                tag.Description ?? string.Empty,
                tag.LucideIconName ?? string.Empty
            );
        }

        private static BlogCacheArticleSummaryDto ToArticleSummaryDto(
            NotionArticle article,
            IReadOnlyList<BlogCacheTagDto> tags)
        {
            return new BlogCacheArticleSummaryDto(
                article.Id,
                article.Title,
                article.Slug,
                article.LastEditedTime,
                article.NotionArticleTags
                    .Select(articleTag => tags.First(tag => tag.Id == articleTag.TagId))
                    .ToList(),
                article.NotionArticleSubTags
                    .Select(articleSubTag => new BlogCacheSubTagDto(
                        articleSubTag.SubTag.Id,
                        articleSubTag.SubTag.Title ?? string.Empty,
                        articleSubTag.SubTag.Slug ?? string.Empty
                    ))
                    .ToList()
            );
        }

        private static BlogCacheArticleDetailDto ToArticleDetailDto(
            NotionArticle article,
            IReadOnlyList<BlogCacheTagDto> tags)
        {
            var summary = ToArticleSummaryDto(article, tags);

            return new BlogCacheArticleDetailDto(
                article.Article?.Content ?? string.Empty,
                article.Title,
                article.Slug,
                article.LastEditedTime,
                article.Id,
                article.Description ?? string.Empty,
                summary.Tags,
                summary.SubTags
            );
        }
    }
}
```

- [ ] **步骤 3: 验证发布服务编译**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 4: 提交发布服务**

运行:

```bash
git add ../Notion.Sync.Api/Business/IServices/IBlogCachePublisher.cs ../Notion.Sync.Api/Business/Services/BlogCachePublisher.cs
git commit -m "feat: publish blog cache to cloudflare kv"
```

## 任务 9: 接入 Hangfire 同步任务

**文件:**
- 修改: `../Notion.Sync.Api/Job/NotionDatabaseSyncJobService.cs`

- [ ] **步骤 1: 注入发布服务**

将 `NotionDatabaseSyncJobService` 构造函数改为:

```csharp
public class NotionDatabaseSyncJobService(
    HttpClient httpClient,
    IConfiguration configuration,
    ITagService tagService,
    INotionArticleService notionArticleService,
    IBlogCachePublisher blogCachePublisher,
    ILogger<NotionDatabaseSyncJobService> logger)
{
```

- [ ] **步骤 2: 让 Node 同步失败时抛出异常**

将 `InvokeNodejs()` 的 catch 改为记录后继续抛出:

```csharp
catch (HttpRequestException ex)
{
    logger.LogError(ex, "HTTP request to Node.js sync service failed.");
    throw;
}
catch (TaskCanceledException ex)
{
    logger.LogError(ex, "Node.js sync service request timed out.");
    throw;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error while invoking Node.js sync service.");
    throw;
}
```

- [ ] **步骤 3: 在同步成功后发布 KV**

在 `SyncTagsAndArticleListAsync()` 的 Node 同步段落后加入:

```csharp
logger.LogInformation("Publishing blog cache to Cloudflare KV.");
await blogCachePublisher.PublishAsync(CancellationToken.None);
logger.LogInformation("Cloudflare KV blog cache published successfully.");
```

最终顺序保持为:

```text
1. 同步 tags 和 subTags
2. 同步 article list
3. 生产环境调用 Node.js 服务更新文章内容
4. 发布 Cloudflare KV
```

- [ ] **步骤 4: 验证同步服务编译**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 5: 提交 Hangfire 接入**

运行:

```bash
git add ../Notion.Sync.Api/Job/NotionDatabaseSyncJobService.cs
git commit -m "feat: publish kv cache after notion sync"
```

## 任务 10: 端到端验证

**文件:**
- 不修改文件

- [ ] **步骤 1: 验证客户端构建**

运行:

```bash
pnpm exec tsc --noEmit
pnpm run lint
pnpm cf:build
```

期望:

```text
无 TypeScript error
lint 通过
OpenNext Cloudflare build 成功
```

- [ ] **步骤 2: 验证同步服务构建**

运行:

```bash
dotnet build ../Notion.Sync.Api/Notion.Sync.Api.csproj
```

期望:

```text
Build succeeded
```

- [ ] **步骤 3: 手动执行 Hangfire job**

在开发环境运行同步服务后，从 Hangfire Dashboard 触发 `SyncTagsAndArticleListAsync`。

期望日志包含:

```text
Publishing blog cache to Cloudflare KV.
Published blog cache version
Cloudflare KV blog cache published successfully.
```

- [ ] **步骤 4: 验证 KV active 指针**

运行:

```bash
pnpm exec wrangler kv key get "blog:active" --binding BLOG_CACHE
```

期望输出为当前版本:

```text
2026-04-19T10-30-00Z
```

- [ ] **步骤 5: 验证前台读取**

运行:

```bash
pnpm cf:preview
```

访问:

```text
/
/blog
/tag
/server-sitemap.xml
```

期望:

```text
页面返回 200，内容来自 Cloudflare KV，服务端日志不出现 Supabase 前台读取。
```

- [ ] **步骤 6: 验证无 revalidate 入口**

运行:

```bash
find src/app/api -maxdepth 3 -type f
```

期望:

```text
不存在 src/app/api/revalidate/route.ts
```

## 自检

- 需求覆盖: 计划覆盖了不暴露 revalidate、AWS Hangfire 直接写 Cloudflare KV、前台只读 KV、Next 短 TTL 自然刷新、Supabase 前台移除和端到端验证。
- AWS 对接: 计划明确了 Cloudflare KV 配置来源、REST API 请求样式、成功响应 schema、C# `HttpClient` 封装、DTO schema、发布顺序和 Hangfire 接入点。
- 空洞步骤检查: 计划没有留待补全的实现步骤。Cloudflare namespace id、account id、namespace id、api token 来自实际环境，不在代码或计划中硬编码。
- 类型一致性: TypeScript 的 `ArticlesType`、`GetTagsAndRecommendArticlesResponseType`、`GetTagsWithArticlesResponseType`、`GetArticleWithSubTagsResponseType` 与 .NET DTO 的 camelCase JSON 输出一致。
- 范围检查: 计划不新增额外部署，不改 UI，不手动操作 OpenNext R2 incremental cache，不引入 Redis。
