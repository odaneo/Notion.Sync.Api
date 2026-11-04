import { defineCloudflareConfig } from "@opennextjs/cloudflare";
import r2IncrementalCache from "@opennextjs/cloudflare/overrides/incremental-cache/r2-incremental-cache";
import { withRegionalCache } from "@opennextjs/cloudflare/overrides/incremental-cache/regional-cache";
import { blogTagCache } from "./cloudflare-cache";

export default defineCloudflareConfig({
  incrementalCache: withRegionalCache(r2IncrementalCache, {
    mode: "long-lived",
    // Always check the invalidation timestamps, including while a global purge propagates.
    bypassTagCacheOnCacheHit: false,
    shouldLazilyUpdateOnCacheHit: false,
  }),
  tagCache: blogTagCache,
  enableCacheInterception: true,
});
