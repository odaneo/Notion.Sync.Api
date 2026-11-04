import assert from "node:assert/strict";
import { test, type TestContext } from "node:test";

function configureEnv(t: TestContext, values: Record<string, string>) {
  const previous = process.env;
  process.env = { ...previous, ...values };
  t.after(() => {
    process.env = previous;
  });
}
import { articlePath, jsonLd } from "../src/lib/blog-path.ts";
import { readSupabase, BLOG_CACHE_TAG } from "../src/lib/supabase.ts";
import { authorized, purgeCloudflare } from "../src/lib/revalidation.ts";
import {
  remarkArticleLinks,
  resolveArticleLink,
} from "../src/lib/markdown-links.ts";

const references = [
  { slug: "hello-world", title: "Hello", sourcePath: "FE/Hello.md" },
  {
    slug: "standalone",
    title: "Standalone",
    sourcePath: "Other/Standalone.md",
  },
];

test("article URLs do not depend on categories and JSON-LD cannot close its script", () => {
  assert.equal(articlePath("文章 1"), "/blog/%E6%96%87%E7%AB%A0%201");
  assert.equal(
    jsonLd({ title: "</script><script>alert(1)</script>" }).includes("<"),
    false,
  );
});

test("published note references support short names, paths, relative links and aliases", () => {
  assert.equal(
    resolveArticleLink("Hello", "Other/Current.md", references),
    "/blog/hello-world",
  );
  assert.equal(
    resolveArticleLink(
      "../FE/Hello.md#Example",
      "Other/Current.md",
      references,
    ),
    "/blog/hello-world#Example",
  );
  assert.equal(
    resolveArticleLink("Private", "Other/Current.md", references),
    null,
  );
  assert.equal(
    resolveArticleLink("Hello", "Current.md", [
      ...references,
      { ...references[0], sourcePath: "Other/Hello.md" },
    ]),
    null,
  );
  const tree = {
    type: "root",
    children: [
      {
        type: "paragraph",
        children: [{ type: "text", value: "[[Hello|显示名称]] [[Private]]" }],
      },
      { type: "code", value: "[[Hello]]" },
    ],
  };
  remarkArticleLinks(references, "Other/Current.md")()(tree);
  assert.deepEqual(tree.children[0].children![0], {
    type: "link",
    url: "/blog/hello-world",
    children: [{ type: "text", value: "显示名称" }],
  });
  assert.equal(tree.children[1].value, "[[Hello]]");
});

test("Supabase reads select the schema, cache by tag without a TTL and paginate", async (t) => {
  configureEnv(t, {
    SUPABASE_URL: "https://example.supabase.co",
    SUPABASE_SCHEMA: "blog_test",
    SUPABASE_PUBLISHABLE_KEY: "publishable-test",
  });
  const offsets: number[] = [];
  t.mock.method(
    globalThis,
    "fetch",
    async (
      input: string | URL,
      init: RequestInit & { next?: { revalidate: boolean; tags: string[] } },
    ) => {
      const url = new URL(input);
      offsets.push(Number(url.searchParams.get("offset")));
      assert.equal(
        new Headers(init.headers).get("Accept-Profile"),
        "blog_test",
      );
      assert.equal(new Headers(init.headers).get("apikey"), "publishable-test");
      assert.equal(init.cache, "force-cache");
      assert.deepEqual(init.next, {
        revalidate: false,
        tags: [BLOG_CACHE_TAG],
      });
      return Response.json(
        offsets.length === 1
          ? Array.from({ length: 500 }, (_, id) => ({ id }))
          : [{ id: 500 }],
      );
    },
  );
  assert.equal((await readSupabase("Articles", "Id")).length, 501);
  assert.deepEqual(offsets, [0, 500]);
});

test("Supabase errors fail visibly instead of returning or caching an empty blog", async (t) => {
  configureEnv(t, {
    SUPABASE_URL: "https://example.supabase.co",
    SUPABASE_SCHEMA: "blog_test",
    SUPABASE_PUBLISHABLE_KEY: "publishable-test",
  });
  t.mock.method(
    globalThis,
    "fetch",
    async () => new Response("private diagnostic", { status: 403 }),
  );
  await assert.rejects(() => readSupabase("Articles", "Id"), /HTTP 403/);
});

test("refresh authentication fails closed for missing, short or incorrect tokens", () => {
  const token = "a".repeat(40);
  assert.equal(authorized(`Bearer ${token}`, token), true);
  for (const header of [
    null,
    token,
    `Bearer ${token} `,
    `Bearer ${"b".repeat(40)}`,
    "Bearer short",
  ]) {
    assert.equal(authorized(header, token), false);
  }
  assert.equal(authorized(`Bearer ${token}`, undefined), false);
  assert.equal(authorized("Bearer short", "short"), false);
});

test("Cloudflare purge requires credentials, rejects redirects and requires a successful JSON acknowledgement", async (t) => {
  configureEnv(t, {
    CACHE_PURGE_ZONE_ID: "test-zone",
    CACHE_PURGE_API_TOKEN: "test-purge-token",
  });
  let succeed = false;
  let status = 200;
  t.mock.method(
    globalThis,
    "fetch",
    async (input: string | URL, init: RequestInit) => {
      assert.equal(
        String(input),
        "https://api.cloudflare.com/client/v4/zones/test-zone/purge_cache",
      );
      assert.deepEqual(JSON.parse(String(init.body)), {
        tags: [BLOG_CACHE_TAG],
      });
      assert.equal(init.redirect, "manual");
      if (status !== 200)
        return new Response("<html>Not a JSON acknowledgement</html>", {
          status,
          headers: { Location: "https://example.com/redirect-target" },
        });
      return Response.json({ success: succeed });
    },
  );
  await assert.rejects(() => purgeCloudflare([BLOG_CACHE_TAG]), /purge failed/);
  succeed = true;
  await purgeCloudflare([BLOG_CACHE_TAG]);
  for (status of [302, 403, 503]) {
    await assert.rejects(
      () => purgeCloudflare([BLOG_CACHE_TAG]),
      new RegExp(`HTTP ${status}`),
    );
  }
  process.env.CACHE_PURGE_API_TOKEN = "";
  await assert.rejects(
    () => purgeCloudflare([BLOG_CACHE_TAG]),
    /not configured/,
  );
});
