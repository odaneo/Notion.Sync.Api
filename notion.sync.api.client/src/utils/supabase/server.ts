import "server-only";
import { createClient } from "@supabase/supabase-js";
import { unstable_cache } from "next/cache";
import {
  GetArticleWithSubTagsResponseType,
  GetTagsAndRecommendArticlesResponseType,
  GetTagsWithArticlesResponseType,
  TagsType,
  ArticlesType,
} from "@/type/api.type";

export const supabase = createClient(
  process.env.SUPABASE_URL!,
  process.env.SUPABASE_SECRET_KEY!,
);

const CACHE_REVALIDATE = 3600;

export const getTagsAndRecommendArticles = unstable_cache(
  async () => {
    const { data } = await supabase
      .rpc("get_tags_and_recommend_articles")
      .single<GetTagsAndRecommendArticlesResponseType>();

    return data ?? null;
  },
  ["home-data"],
  { revalidate: CACHE_REVALIDATE },
);

export const getAllArticles = unstable_cache(
  async () => {
    const { data } = await supabase
      .rpc("get_all_articles")
      .overrideTypes<ArticlesType[]>();

    return data ?? null;
  },
  ["article-list"],
  { revalidate: CACHE_REVALIDATE },
);

export const getTagsWithArticles = unstable_cache(
  async () => {
    const { data } = await supabase
      .rpc("get_tags_with_articles_json")
      .overrideTypes<GetTagsWithArticlesResponseType[]>();

    return data ?? null;
  },
  ["tag-list"],
  { revalidate: CACHE_REVALIDATE },
);

export const getArticleWithSubTags = unstable_cache(
  async (slug: string) => {
    if (!slug) {
      return null;
    }

    const { data } = await supabase
      .rpc("get_article_by_slug", { article_slug: slug })
      .single()
      .overrideTypes<GetArticleWithSubTagsResponseType>();

    return data ?? null;
  },
  ["article-detail"],
  { revalidate: CACHE_REVALIDATE },
);

export const getTagDetailWithArticles = unstable_cache(
  async (tag: string) => {
    if (!tag) {
      return null;
    }

    const { data } = await supabase
      .rpc("get_tag_detail_with_articles", { tag_slug: tag })
      .single<{ tag: TagsType; articles: ArticlesType[] }>();

    return data ?? null;
  },
  ["tag-detail"],
  { revalidate: CACHE_REVALIDATE },
);
