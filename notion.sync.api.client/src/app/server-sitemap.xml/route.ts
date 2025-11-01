import { getServerSideSitemap } from "next-sitemap";
import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";

export async function GET() {
  const { data: tagsData } = await supabase
    .rpc("get_tags_with_articles_json")
    .overrideTypes<GetTagsWithArticlesResponseType[]>();
  const tags = Array.isArray(tagsData) ? tagsData : [];

  const fields = [
    { loc: `${process.env.HOME_URL}`, lastmod: new Date().toISOString() },
  ];

  tags.forEach((tag) => {
    tag.Articles?.forEach((article) => {
      fields.push({
        loc: `${process.env.HOME_URL}/${encodeURIComponent(
          tag.Slug
        )}/${encodeURIComponent(article.Slug)}`,
        lastmod: article.LastEditedTime
          ? new Date(article.LastEditedTime).toISOString()
          : new Date().toISOString(),
      });
    });
  });

  const res = await getServerSideSitemap(fields);
  res.headers.set(
    "Cache-Control",
    "public, max-age=600, s-maxage=600, stale-while-revalidate=60"
  );
  return res;
}
