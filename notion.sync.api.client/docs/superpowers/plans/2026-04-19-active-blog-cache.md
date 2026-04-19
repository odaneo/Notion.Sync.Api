# 主动博客缓存 Implementation Plan

> **给 agentic workers:** REQUIRED SUB-SKILL: 使用 superpowers:subagent-driven-development 或 superpowers:executing-plans 按任务逐项执行。步骤使用 checkbox 语法跟踪。

**目标:** 将博客前台读取链路从 Supabase RPC 改为 Cloudflare KV 主动缓存，并用受保护的 revalidate 入口让 AWS 同步完成后主动刷新 Next.js 页面缓存。

**架构:** Supabase 继续作为事实数据库，AWS 同步任务在写入 Supabase 后生成前台所需 JSON 并发布到 Cloudflare KV。Next.js 页面只读 KV，不再在用户请求路径访问 Supabase；OpenNext 现有 R2 incremental cache 继续负责 HTML/ISR 缓存，通过 revalidate 主动失效。

**技术栈:** Next.js 15 App Router、React 19、TypeScript、OpenNext Cloudflare、Cloudflare Workers KV、Cloudflare R2 incremental cache、Wrangler、pnpm。

---

## 范围和约束

- 只修改 `notion.sync.api.client` 仓库内文件。
- 不改 UI 样式和现有中文文案。
- 不改 Notion 渲染组件。
- 不新增 Redis、独立 Worker、独立服务或额外部署单元。
- 不手动写 OpenNext 的 R2 incremental cache 内部对象。
- 不在前台请求路径保留 Supabase 兜底查询。
- AWS 同步任务代码当前不在本仓库，本计划只定义它需要写入 KV 和调用 revalidate 的契约。

## 文件结构计划

- 创建 `src/type/blog-cache.type.ts`
  - 定义 KV 缓存 manifest、标签详情、发布 payload 相关类型。
- 创建 `src/utils/blog-cache/server.ts`
  - 通过 `getCloudflareContext({ async: true })` 读取 `BLOG_CACHE` binding。
  - 导出页面现有需要的读取函数。
  - 使用 `blog:active` 指向当前版本，读取 `blog:v:<version>:*` 的不可变 JSON。
- 修改 `src/app/page.tsx`
  - 将首页数据导入来源从 Supabase 工具改为 KV 缓存工具。
- 修改 `src/app/blog/page.tsx`
  - 将文章列表数据导入来源改为 KV 缓存工具。
- 修改 `src/app/blog/[tag]/[slug]/page.tsx`
  - 将文章详情数据导入来源改为 KV 缓存工具。
- 修改 `src/app/tag/page.tsx`
  - 将标签列表数据导入来源改为 KV 缓存工具。
- 修改 `src/app/tag/[tag]/page.tsx`
  - 将标签详情数据导入来源改为 KV 缓存工具。
- 修改 `src/app/server-sitemap.xml/route.ts`
  - 使用 KV 中的标签文章列表生成 sitemap，不再直接访问 Supabase。
- 创建 `src/app/api/revalidate/route.ts`
  - 提供 AWS 同步任务调用的受保护 revalidate 入口。
- 修改 `wrangler.jsonc`
  - 增加 `BLOG_CACHE` KV namespace binding。
- 修改 `.dev.vars.example`
  - 增加 `CACHE_REVALIDATE_SECRET` 示例值，后续移除 Supabase 示例变量。
- 重新生成 `cloudflare-env.d.ts`
  - 让 `Cloudflare.Env` 包含 `BLOG_CACHE` 和 `CACHE_REVALIDATE_SECRET`。
- 修改 `package.json` 和 `pnpm-lock.yaml`
  - 当前前台不再使用 Supabase client 后，移除 `@supabase/supabase-js` 和 `@supabase/ssr`。
- 删除 `src/utils/supabase/server.ts`
  - 所有前台导入切到 `src/utils/blog-cache/server.ts` 后删除。

## KV 数据契约

AWS 同步任务每次发布一个不可变版本，最后更新 active 指针。

```text
blog:active = 2026-04-19T10-30-00Z
blog:v:2026-04-19T10-30-00Z:manifest
blog:v:2026-04-19T10-30-00Z:home
blog:v:2026-04-19T10-30-00Z:articles
blog:v:2026-04-19T10-30-00Z:tags
blog:v:2026-04-19T10-30-00Z:tag:<tagSlug>
blog:v:2026-04-19T10-30-00Z:article:<articleSlug>
```

`manifest` 示例:

```json
{
  "version": "2026-04-19T10-30-00Z",
  "generatedAt": "2026-04-19T10:30:00.000Z",
  "paths": [
    "/",
    "/blog",
    "/tag",
    "/tag/nextjs",
    "/blog/nextjs/example-post",
    "/server-sitemap.xml"
  ]
}
```

## 任务 1: 添加 Cloudflare KV binding

**文件:**
- 修改: `wrangler.jsonc`
- 修改: `.dev.vars.example`
- 生成: `cloudflare-env.d.ts`

- [ ] **步骤 1: 创建 production KV namespace**

运行:

```bash
pnpm exec wrangler kv namespace create BLOG_CACHE
```

期望输出包含一个 production namespace id:

```text
id = "<wrangler 输出的 production id>"
```

- [ ] **步骤 2: 创建 preview KV namespace**

运行:

```bash
pnpm exec wrangler kv namespace create BLOG_CACHE --preview
```

期望输出包含一个 preview namespace id:

```text
preview_id = "<wrangler 输出的 preview id>"
```

- [ ] **步骤 3: 修改 Wrangler 配置**

在 `wrangler.jsonc` 顶层加入 `kv_namespaces`，保留现有 `r2_buckets`、`durable_objects`、`migrations` 和 `services` 配置:

```jsonc
{
  "$schema": "./node_modules/wrangler/config-schema.json",
  "name": "notion-sync-api-client",
  "main": ".open-next/worker.js",
  "compatibility_date": "2026-04-15",
  "compatibility_flags": ["nodejs_compat"],
  "assets": {
    "directory": ".open-next/assets",
    "binding": "ASSETS"
  },
  "services": [
    {
      "binding": "WORKER_SELF_REFERENCE",
      "service": "notion-sync-api-client"
    }
  ],
  "kv_namespaces": [
    {
      "binding": "BLOG_CACHE",
      "id": "步骤 1 输出的 production id",
      "preview_id": "步骤 2 输出的 preview id"
    }
  ],
  "r2_buckets": [
    {
      "binding": "NEXT_INC_CACHE_R2_BUCKET",
      "bucket_name": "notion-sync-api-client-cache"
    }
  ],
  "durable_objects": {
    "bindings": [
      {
        "name": "NEXT_CACHE_DO_QUEUE",
        "class_name": "DOQueueHandler"
      }
    ]
  },
  "migrations": [
    {
      "tag": "v1",
      "new_sqlite_classes": ["DOQueueHandler"]
    }
  ],
  "observability": {
    "enabled": false,
    "head_sampling_rate": 1,
    "logs": {
      "enabled": true,
      "head_sampling_rate": 1,
      "persist": true,
      "invocation_logs": true
    },
    "traces": {
      "enabled": true,
      "persist": true,
      "head_sampling_rate": 1
    }
  }
}
```

- [ ] **步骤 4: 配置 revalidate secret**

运行:

```bash
pnpm exec wrangler secret put CACHE_REVALIDATE_SECRET
```

输入一个长随机字符串。AWS 同步任务调用 `/api/revalidate` 时使用同一个值。

- [ ] **步骤 5: 更新本地环境示例**

将 `.dev.vars.example` 改为:

```text
HOME_URL=http://localhost:3000
CACHE_REVALIDATE_SECRET=replace-with-local-secret
```

本地 `.dev.vars` 和 `.env.local` 也保留同名变量，但不要提交真实 secret。

- [ ] **步骤 6: 重新生成 Cloudflare 类型**

运行:

```bash
pnpm run cf:typegen
```

期望 `cloudflare-env.d.ts` 顶部的 `Cloudflare.Env` 至少包含:

```ts
interface Env {
  ASSETS: Fetcher;
  BLOG_CACHE: KVNamespace;
  CACHE_REVALIDATE_SECRET: string;
  HOME_URL: string;
}
```

- [ ] **步骤 7: 验证配置**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 8: 提交 Cloudflare binding 变更**

运行:

```bash
git add wrangler.jsonc .dev.vars.example cloudflare-env.d.ts
git commit -m "chore: add blog cache kv binding"
```

## 任务 2: 定义 AWS 同步任务发布契约

**文件:**
- 本仓库无 AWS 同步任务代码需要修改

- [ ] **步骤 1: 在同步完成后发布 KV**

AWS 同步任务完成远端拉取并写入 Supabase 后，按这个顺序写 Cloudflare KV:

```text
1. 生成 version，例如 2026-04-19T10-30-00Z
2. 写 blog:v:<version>:home
3. 写 blog:v:<version>:articles
4. 写 blog:v:<version>:tags
5. 写每个 blog:v:<version>:tag:<tagSlug>
6. 写每个 blog:v:<version>:article:<articleSlug>
7. 写 blog:v:<version>:manifest
8. 最后写 blog:active = <version>
```

期望:

```text
用户请求要么读到旧 active version，要么读到新 active version，不会读到一半发布的新数据。
```

- [ ] **步骤 2: 使用 Cloudflare KV REST API 写入**

AWS 同步任务侧使用 Cloudflare API token 写入 KV。token 至少需要目标 account 下的 Workers KV Storage 写权限。

单 key 写入请求形态:

```http
PUT https://api.cloudflare.com/client/v4/accounts/<account_id>/storage/kv/namespaces/<namespace_id>/values/<key>
Authorization: Bearer <cloudflare_api_token>
Content-Type: application/json

<json value>
```

批量写入时使用 Cloudflare KV bulk update API。批量失败返回 unsuccessful keys 时，只重试失败 key。

- [ ] **步骤 3: 保留旧版本**

AWS 同步任务每次发布后保留最近 3 个 version，删除更旧的 `blog:v:<oldVersion>:*` key。

期望:

```text
Cloudflare 边缘节点短时间读到旧 active version 时，旧 version 的内容仍然存在。
```

- [ ] **步骤 4: 记录发布日志**

AWS 同步任务每次发布至少记录这些字段:

```json
{
  "version": "2026-04-19T10-30-00Z",
  "home": true,
  "articles": 42,
  "tags": 8,
  "articleDetails": 42,
  "publishedActiveVersion": true
}
```

## 任务 3: 添加 KV 缓存读取层

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
  paths: string[];
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

## 任务 4: 切换前台页面数据来源

**文件:**
- 修改: `src/app/page.tsx`
- 修改: `src/app/blog/page.tsx`
- 修改: `src/app/blog/[tag]/[slug]/page.tsx`
- 修改: `src/app/tag/page.tsx`
- 修改: `src/app/tag/[tag]/page.tsx`
- 修改: `src/app/server-sitemap.xml/route.ts`

- [ ] **步骤 1: 修改首页导入**

将 `src/app/page.tsx` 第一行改为:

```ts
import { getTagsAndRecommendArticles } from "@/utils/blog-cache/server";
```

- [ ] **步骤 2: 修改文章列表导入**

将 `src/app/blog/page.tsx` 第一行改为:

```ts
import { getAllArticles } from "@/utils/blog-cache/server";
```

- [ ] **步骤 3: 修改文章详情导入**

将 `src/app/blog/[tag]/[slug]/page.tsx` 第一行改为:

```ts
import { getArticleWithSubTags } from "@/utils/blog-cache/server";
```

- [ ] **步骤 4: 修改标签列表导入**

将 `src/app/tag/page.tsx` 第一行改为:

```ts
import { getTagsWithArticles } from "@/utils/blog-cache/server";
```

- [ ] **步骤 5: 修改标签详情导入**

将 `src/app/tag/[tag]/page.tsx` 第一行改为:

```ts
import { getTagDetailWithArticles } from "@/utils/blog-cache/server";
```

- [ ] **步骤 6: 修改 sitemap 数据来源**

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
    "public, max-age=600, s-maxage=600, stale-while-revalidate=60",
  );
  return res;
}
```

- [ ] **步骤 7: 确认没有页面导入 Supabase 工具**

运行:

```bash
rg "@/utils/supabase/server|supabase" src/app src/components src/utils
```

期望:

```text
只允许出现 src/utils/supabase/server.ts 自身，页面和 route 不再出现 Supabase 读取。
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
git add src/app/page.tsx src/app/blog/page.tsx src/app/blog/[tag]/[slug]/page.tsx src/app/tag/page.tsx src/app/tag/[tag]/page.tsx src/app/server-sitemap.xml/route.ts
git commit -m "feat: serve blog pages from kv cache"
```

## 任务 5: 添加同步后 revalidate 调用契约

**文件:**
- 创建: `src/app/api/revalidate/route.ts`
- 本仓库无 AWS 同步任务代码需要修改

- [ ] **步骤 1: 创建受保护 revalidate route**

创建 `src/app/api/revalidate/route.ts`:

```ts
import { revalidatePath } from "next/cache";
import { NextRequest, NextResponse } from "next/server";

type RevalidateRequestBody = {
  paths?: unknown;
};

export async function POST(request: NextRequest) {
  const authorization = request.headers.get("authorization");

  if (authorization !== `Bearer ${process.env.CACHE_REVALIDATE_SECRET}`) {
    return NextResponse.json({ message: "Unauthorized" }, { status: 401 });
  }

  const body = (await request.json()) as RevalidateRequestBody;
  const paths = Array.isArray(body.paths)
    ? body.paths.filter(
        (path): path is string =>
          typeof path === "string" && path.startsWith("/"),
      )
    : [];

  paths.forEach((path) => {
    revalidatePath(path);
  });

  return NextResponse.json({
    revalidated: paths.length,
    paths,
  });
}
```

- [ ] **步骤 2: 验证 route 类型**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 3: 定义 AWS 调用请求**

AWS 同步任务在 `blog:active` 写入成功后调用:

```http
POST https://<site-host>/api/revalidate
Authorization: Bearer <CACHE_REVALIDATE_SECRET>
Content-Type: application/json

{
  "paths": [
    "/",
    "/blog",
    "/tag",
    "/server-sitemap.xml",
    "/tag/nextjs",
    "/blog/nextjs/example-post"
  ]
}
```

路径来源优先使用 `blog:v:<version>:manifest` 里的 `paths`。

- [ ] **步骤 4: 定义 AWS 预热请求**

AWS 同步任务收到 revalidate 成功响应后，对同一批 `paths` 发 GET 请求。预热失败不回滚 `blog:active`，只记录失败 URL，下一次同步继续覆盖。

期望日志字段:

```json
{
  "version": "2026-04-19T10-30-00Z",
  "revalidated": 6,
  "warmupRequested": 6,
  "warmupFailed": []
}
```

- [ ] **步骤 5: 提交 revalidate route**

运行:

```bash
git add src/app/api/revalidate/route.ts
git commit -m "feat: add blog cache revalidate endpoint"
```

## 任务 6: 移除前台 Supabase 依赖

**文件:**
- 删除: `src/utils/supabase/server.ts`
- 修改: `.dev.vars.example`
- 修改: `package.json`
- 修改: `pnpm-lock.yaml`
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

- [ ] **步骤 3: 移除 Supabase 前端依赖**

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

- [ ] **步骤 4: 从 Cloudflare 环境移除 Supabase 类型**

将 `.dev.vars.example` 保持为:

```text
HOME_URL=http://localhost:3000
CACHE_REVALIDATE_SECRET=replace-with-local-secret
```

本地 `.dev.vars` 和 `.env.local` 删除 `SUPABASE_URL`、`SUPABASE_SECRET_KEY`，保留 `HOME_URL`、`CACHE_REVALIDATE_SECRET`。这些本地文件不提交。

如果 Cloudflare dashboard 中还配置了 `SUPABASE_URL` 和 `SUPABASE_SECRET_KEY`，在部署确认不再访问 Supabase 后删除这两个变量。

运行:

```bash
pnpm run cf:typegen
```

期望 `cloudflare-env.d.ts` 顶部的 `ProcessEnv` 不再包含:

```ts
SUPABASE_URL
SUPABASE_SECRET_KEY
```

- [ ] **步骤 5: 验证没有 Supabase 残留导入**

运行:

```bash
rg "supabase|SUPABASE|@supabase" src package.json cloudflare-env.d.ts
```

期望:

```text
无输出
```

- [ ] **步骤 6: 验证类型和 lint**

运行:

```bash
pnpm exec tsc --noEmit
pnpm run lint
```

期望:

```text
无 TypeScript error
lint 通过
```

- [ ] **步骤 7: 提交 Supabase 移除**

运行:

```bash
git add package.json pnpm-lock.yaml .dev.vars.example cloudflare-env.d.ts src/utils/supabase/server.ts
git commit -m "chore: remove frontend supabase reads"
```

## 任务 7: 本地和 Cloudflare 构建验证

**文件:**
- 不修改文件

- [ ] **步骤 1: 类型检查**

运行:

```bash
pnpm exec tsc --noEmit
```

期望:

```text
无 TypeScript error
```

- [ ] **步骤 2: lint**

运行:

```bash
pnpm run lint
```

期望:

```text
lint 通过
```

- [ ] **步骤 3: Next 构建**

运行:

```bash
pnpm build
```

期望:

```text
Compiled successfully
```

- [ ] **步骤 4: OpenNext Cloudflare 构建**

运行:

```bash
pnpm cf:build
```

期望:

```text
OpenNext build 完成，生成 .open-next/worker.js
```

- [ ] **步骤 5: 预览 smoke test**

运行:

```bash
pnpm cf:preview
```

在预览服务启动后访问:

```text
/
/blog
/tag
/server-sitemap.xml
```

期望:

```text
页面返回 200；如果 KV 没有测试数据，页面可以为空列表，但不能访问 Supabase。
```

- [ ] **步骤 6: 提交验证记录**

如果验证过程需要修改配置文件，运行:

```bash
git add <实际修改的文件>
git commit -m "fix: complete blog cache cloudflare build"
```

如果没有文件修改，不创建空提交。

## 自检

- Spec coverage: 计划覆盖了 KV binding、AWS 发布契约、Next 数据读取迁移、sitemap 迁移、revalidate 入口、同步后预热契约、Supabase 前台移除和构建验证。
- 空洞步骤检查: 计划没有留待补全的实现步骤。Cloudflare namespace id 和 secret 必须来自 Wrangler 实际输出，不能在计划阶段硬编码。
- Type consistency: `BlogCacheManifestType`、`TagDetailWithArticlesType`、`getTagsAndRecommendArticles`、`getAllArticles`、`getTagsWithArticles`、`getArticleWithSubTags`、`getTagDetailWithArticles` 在类型文件、缓存读取层和页面导入中名称一致。
- Scope check: AWS 同步任务代码不在本仓库，因此任务 2 和任务 5 只定义契约，不要求修改不存在的文件。
