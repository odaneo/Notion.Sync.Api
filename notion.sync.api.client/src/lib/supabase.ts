export const BLOG_CACHE_TAG = "blog-content";

export type ArticleRow = {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  lastEditedTime: string;
  sourcePath: string;
  recommend: boolean;
  articleTags: { tag: TagRow }[];
  articleSubTags: { subTag: { id: string; title: string; slug: string } }[];
};

export type TagRow = {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  lucideIconName: string | null;
};

export const tagSelect =
  "id:Id,title:Title,slug:Slug,description:Description,lucideIconName:LucideIconName";
export const articleSelect = `id:Id,title:Title,slug:Slug,description:Description,lastEditedTime:LastEditedTime,sourcePath:SourcePath,recommend:Recommend,articleTags:ArticleTags(tag:Tags(${tagSelect})),articleSubTags:ArticleSubTags(subTag:SubTags(id:Id,title:Title,slug:Slug))`;

export async function readSupabase<T>(
  table: string,
  select: string,
  filters: Record<string, string> = {},
): Promise<T[]> {
  const { SUPABASE_URL, SUPABASE_SCHEMA, SUPABASE_PUBLISHABLE_KEY } =
    process.env;
  if (!SUPABASE_URL || !SUPABASE_SCHEMA || !SUPABASE_PUBLISHABLE_KEY) {
    throw new Error(
      "Configure SUPABASE_URL, SUPABASE_SCHEMA and SUPABASE_PUBLISHABLE_KEY.",
    );
  }
  const rows: T[] = [];
  const pageSize = 500;
  for (let offset = 0; ; offset += pageSize) {
    const url = new URL(`${SUPABASE_URL.replace(/\/$/, "")}/rest/v1/${table}`);
    url.search = new URLSearchParams({
      select,
      order: "Id.asc",
      limit: String(pageSize),
      offset: String(offset),
      ...filters,
    }).toString();
    const response = await fetch(url, {
      headers: {
        apikey: SUPABASE_PUBLISHABLE_KEY,
        "Accept-Profile": SUPABASE_SCHEMA,
      },
      cache: "force-cache",
      next: { revalidate: false, tags: [BLOG_CACHE_TAG] },
    });
    if (!response.ok)
      throw new Error(
        `Supabase ${table} read failed (HTTP ${response.status}).`,
      );
    const page: T[] = await response.json();
    if (!Array.isArray(page))
      throw new Error(`Supabase ${table} returned invalid data.`);
    rows.push(...page);
    if (page.length < pageSize) return rows;
  }
}
