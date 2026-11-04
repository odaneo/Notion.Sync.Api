import doShardedTagCache from "@opennextjs/cloudflare/overrides/tag-cache/do-sharded-tag-cache";

// One shard is sufficient for this blog. Keep the same adapter instance/options in the refresh handler.
export const blogTagCache = doShardedTagCache({ baseShardSize: 1 });
