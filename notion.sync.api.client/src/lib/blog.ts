import "server-only";
import { cache } from "react";
import {
  articleSelect,
  readSupabase,
  tagSelect,
  type ArticleRow,
  type TagRow,
} from "./supabase";
import type {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  TagsType,
} from "@/type/api.type";

function mapTag(tag: TagRow, articleCount = 0): TagsType {
  return {
    ...tag,
    articleCount,
    description: tag.description ?? "",
    lucideIconName: tag.lucideIconName ?? "tag",
  };
}

function mapArticle(article: ArticleRow): ArticlesType {
  return {
    id: article.id,
    title: article.title,
    slug: article.slug,
    sourcePath: article.sourcePath,
    lastEditedTime: article.lastEditedTime,
    tags: article.articleTags
      .map(({ tag }) => mapTag(tag))
      .sort((a, b) => a.slug.localeCompare(b.slug)),
    subTags: article.articleSubTags
      .map(({ subTag }) => subTag)
      .sort((a, b) => a.slug.localeCompare(b.slug)),
  };
}

const getArticleRows = cache(() =>
  readSupabase<ArticleRow>("Articles", articleSelect),
);

export const getAllArticles = cache(async () => {
  const articles = (await getArticleRows()).map(mapArticle);
  return articles.sort(
    (a, b) =>
      b.lastEditedTime.localeCompare(a.lastEditedTime) ||
      a.slug.localeCompare(b.slug),
  );
});

export const getTagsAndRecommendArticles = cache(async () => {
  const [rows, articles, tags] = await Promise.all([
    getArticleRows(),
    getAllArticles(),
    readSupabase<TagRow>("Tags", tagSelect),
  ]);
  const recommended = new Set(
    rows.filter((article) => article.recommend).map((article) => article.id),
  );
  return {
    tags: tags
      .map((tag) =>
        mapTag(
          tag,
          articles.filter((article) =>
            article.tags.some(({ id }) => id === tag.id),
          ).length,
        ),
      )
      .filter((tag) => tag.articleCount > 0)
      .sort((a, b) => a.slug.localeCompare(b.slug)),
    recommendArticles: articles.filter((article) =>
      recommended.has(article.id),
    ),
  };
});

export const getTagsWithArticles = cache(async () => {
  const [{ tags }, articles] = await Promise.all([
    getTagsAndRecommendArticles(),
    getAllArticles(),
  ]);
  return tags.map((tag) => ({
    ...tag,
    articles: articles.filter((article) =>
      article.tags.some(({ id }) => id === tag.id),
    ),
  }));
});

export const getTagDetailWithArticles = cache(async (slug: string) => {
  const tag = (await getTagsWithArticles()).find((tag) => tag.slug === slug);
  return tag ? { tag, articles: tag.articles } : null;
});

export const getArticleWithSubTags = cache(
  async (slug: string): Promise<GetArticleWithSubTagsResponseType | null> => {
    const [article] = await readSupabase<ArticleRow & { content: string }>(
      "Articles",
      `${articleSelect},content:Content`,
      { Slug: `eq.${slug}` },
    );
    return article
      ? {
          ...mapArticle(article),
          description: article.description ?? "",
          content: article.content,
        }
      : null;
  },
);
