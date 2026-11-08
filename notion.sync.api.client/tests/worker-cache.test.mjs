import assert from "node:assert/strict";
import { createServer } from "node:http";
import { test } from "node:test";
import { unstable_dev } from "wrangler";

test(
  "Worker caches SSR until an authenticated refresh, including new/deleted articles and sitemap",
  { timeout: 120000 },
  async (t) => {
    let updated = false;
    let apiReads = 0;
    const token = "local-worker-test-token-12345678901234567890";
    const tag = {
      id: "tag-1",
      title: "Primary",
      slug: "primary",
      description: "",
      lucideIconName: "tag",
    };
    const article = (slug, title, tagged = true) => ({
      id: slug,
      title,
      slug,
      description: title,
      lastEditedTime: "2026-11-03T00:00:00Z",
      sourcePath: `${slug}.md`,
      recommend: true,
      articleTags: tagged
        ? [{ tag }, { tag: { ...tag, id: "tag-2", slug: "secondary" } }]
        : [],
      articleSubTags: [
        { subTag: { id: "sub-1", title: "Independent", slug: "independent" } },
      ],
      content: `## ${title}\n\n| Column | Value |\n| --- | --- |\n| Example | SSR |\n\n\`\`\`js\nconst value = 1;\n\`\`\`\n\n<script>alert('unsafe')</script>`,
    });
    const api = createServer((req, res) => {
      apiReads++;
      const url = new URL(req.url, "http://localhost");
      let rows;
      if (url.pathname === "/rest/v1/Tags")
        rows = [
          tag,
          { ...tag, id: "empty-tag", slug: "empty", title: "Empty tag" },
        ];
      else if (url.pathname === "/rest/v1/Articles") {
        rows = [
          article("cache-sample", updated ? "Version two" : "Version one"),
          article("no-category", "No category", false),
          article(
            updated ? "new-article" : "deleted-article",
            updated ? "New article" : "Deleted article",
          ),
        ];
        const filter = url.searchParams.get("Slug");
        if (filter) rows = rows.filter((row) => `eq.${row.slug}` === filter);
      } else {
        res.writeHead(404);
        res.end();
        return;
      }
      assert.equal(req.headers["accept-profile"], "blog_test");
      res.setHeader("Content-Type", "application/json");
      res.end(JSON.stringify(rows));
    });
    await new Promise((resolve) => api.listen(0, "127.0.0.1", resolve));
    t.after(() => new Promise((resolve) => api.close(resolve)));
    const worker = await unstable_dev(".open-next/worker.js", {
      config: "wrangler.jsonc",
      local: true,
      persist: false,
      port: 8788,
      ip: "127.0.0.1",
      inspect: false,
      logLevel: "error",
      vars: {
        HOME_URL: "http://localhost:8788",
        SUPABASE_URL: `http://127.0.0.1:${api.address().port}`,
        SUPABASE_SCHEMA: "blog_test",
        SUPABASE_PUBLISHABLE_KEY: "local-test-key",
        REVALIDATION_TOKEN: token,
        CACHE_PURGE_ZONE_ID: "",
        CACHE_PURGE_API_TOKEN: "",
      },
      experimental: {
        disableExperimentalWarning: true,
        watch: false,
        disableDevRegistry: true,
      },
    });
    t.after(() => worker.stop());
    const get = (path, init) =>
      fetch(`http://localhost:${worker.port}${path}`, init);
    assert.equal(
      (await get("/api/revalidate", { method: "POST" })).status,
      401,
    );
    const before = await (await get("/blog/cache-sample")).text();
    assert.match(before, /Version one/);
    assert.match(before, /<table>/);
    assert.match(before, /hljs-keyword/);
    assert.doesNotMatch(before, /<script>alert\('unsafe'\)<\/script>/);
    assert.equal((await get("/blog/new-article")).status, 404);
    assert.equal((await get("/blog/deleted-article")).status, 200);
    const indexPages = ["/", "/blog", "/tag", "/tag/primary"];
    for (const path of indexPages) {
      const html = await (await get(path)).text();
      assert.match(html, /Version one/, path);
      assert.doesNotMatch(html, /Empty tag/, path);
    }
    await get("/sitemap.xml");
    // R2 writes are awaited by the runtime's waitUntil after the response.
    await new Promise((resolve) => setTimeout(resolve, 100));
    const readsBeforeHit = apiReads;
    updated = true;
    assert.match(await (await get("/blog/cache-sample")).text(), /Version one/);
    for (const path of indexPages) {
      assert.match(await (await get(path)).text(), /Version one/, path);
    }
    assert.equal(
      apiReads,
      readsBeforeHit,
      "cache hits must not query Supabase",
    );
    const refresh = await get("/api/revalidate", {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
    assert.equal(refresh.status, 200);
    assert.deepEqual(await refresh.json(), { revalidated: true });
    assert.match(await (await get("/blog/cache-sample")).text(), /Version two/);
    for (const path of indexPages) {
      const html = await (await get(path)).text();
      assert.match(html, /Version two/, path);
      assert.doesNotMatch(html, /Empty tag/, path);
    }
    assert.equal((await get("/blog/new-article")).status, 200);
    assert.equal((await get("/blog/deleted-article")).status, 404);
    assert.equal((await get("/blog/no-category")).status, 200);
    const sitemap = await (await get("/sitemap.xml")).text();
    assert.doesNotMatch(sitemap, /\/tag\/empty</);
    assert.match(sitemap, /\/blog\/new-article/);
    assert.doesNotMatch(sitemap, /\/blog\/deleted-article/);
    assert.equal(
      (sitemap.match(/\/blog\/cache-sample</g) ?? []).length,
      1,
      "multi-tag articles appear once",
    );
    const legacy = await get("/blog/primary/cache-sample?from=old-link", {
      redirect: "manual",
    });
    assert.equal(legacy.status, 308);
    const destination = new URL(legacy.headers.get("location"), legacy.url);
    assert.equal(destination.pathname, "/blog/cache-sample");
    assert.equal(destination.search, "?from=old-link");
  },
);
