import { revalidatePath, revalidateTag } from "next/cache";
import type { NextRequest } from "next/server";
import { blogTagCache } from "../../../../cloudflare-cache";
import { BLOG_CACHE_TAG } from "@/lib/supabase";
import { authorized, purgeCloudflare } from "@/lib/revalidation";

export async function POST(request: NextRequest) {
  if (
    !process.env.REVALIDATION_TOKEN ||
    process.env.REVALIDATION_TOKEN.length < 32
  ) {
    return Response.json({ revalidated: false }, { status: 503 });
  }
  if (
    !authorized(
      request.headers.get("authorization"),
      process.env.REVALIDATION_TOKEN,
    )
  ) {
    return Response.json({ revalidated: false }, { status: 401 });
  }
  try {
    if ("openNextConfig" in globalThis) {
      // Next queues revalidateTag until after the response. Await the same adapter directly
      // and read back each timestamp so the backend only receives a completed acknowledgement.
      const started = Date.now();
      const tags = [BLOG_CACHE_TAG, "_N_T_/layout"];
      await blogTagCache.writeTags(
        tags.map((tag) => ({ tag, stale: started, expire: started })),
      );
      for (const tag of tags) {
        if ((await blogTagCache.getLastRevalidated([tag])) < started)
          throw new Error("Cache invalidation was not persisted.");
      }
      const local = ["localhost", "127.0.0.1", "[::1]"].includes(
        request.nextUrl.hostname,
      );
      if (!local) await purgeCloudflare(tags);
    } else {
      revalidateTag(BLOG_CACHE_TAG, { expire: 0 });
      revalidatePath("/", "layout");
      revalidatePath("/sitemap.xml");
    }
    return Response.json(
      { revalidated: true },
      { headers: { "Cache-Control": "no-store" } },
    );
  } catch (error) {
    console.error(
      "Blog cache invalidation failed:",
      error instanceof Error ? error.message : "Unknown error",
    );
    return Response.json(
      { revalidated: false },
      {
        status: 503,
        headers: { "Retry-After": "5", "Cache-Control": "no-store" },
      },
    );
  }
}
