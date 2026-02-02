import { getServerSideSitemap } from "next-sitemap";
import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import { Changefreq } from "@/type/sitemap.type";

export async function GET() {
  const { data: tagsData } = await supabase
    .rpc("get_tags_with_articles_json")
    .overrideTypes<GetTagsWithArticlesResponseType[]>();
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
