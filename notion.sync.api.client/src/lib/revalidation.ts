import { timingSafeEqual } from "node:crypto";

export function authorized(header: string | null, token: string | undefined) {
  if (!token || token.length < 32 || !header?.startsWith("Bearer "))
    return false;
  const expected = Buffer.from(token);
  const actual = Buffer.from(header.slice(7));
  return expected.length === actual.length && timingSafeEqual(expected, actual);
}

export async function purgeCloudflare(tags: string[]) {
  const { CACHE_PURGE_ZONE_ID, CACHE_PURGE_API_TOKEN } = process.env;
  if (!CACHE_PURGE_ZONE_ID || !CACHE_PURGE_API_TOKEN)
    throw new Error("Cloudflare purge credentials are not configured.");
  const response = await fetch(
    `https://api.cloudflare.com/client/v4/zones/${CACHE_PURGE_ZONE_ID}/purge_cache`,
    {
      method: "POST",
      redirect: "manual",
      cache: "no-store",
      signal: AbortSignal.timeout(10000),
      headers: {
        Authorization: `Bearer ${CACHE_PURGE_API_TOKEN}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ tags }),
    },
  );
  const result = response.ok
    ? ((await response.json()) as { success?: boolean } | null)
    : null;
  if (!response.ok || result?.success !== true)
    throw new Error(`Cloudflare cache purge failed (HTTP ${response.status}).`);
}
