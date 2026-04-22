import {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  GetTagsAndRecommendArticlesResponseType,
  GetTagsWithArticlesResponseType,
  TagsType,
} from "@/type/api.type";

export type TagDetailWithArticlesType = {
  tag: TagsType;
  articles: ArticlesType[];
};

export type BlogCacheManifestType = {
  version: string;
  generatedAt: string;
  articleCount: number;
  tagCount: number;
  keys: string[];
};

export type BlogCachePayloadType = {
  home: GetTagsAndRecommendArticlesResponseType;
  articles: ArticlesType[];
  tags: GetTagsWithArticlesResponseType[];
  tagDetails: Record<string, TagDetailWithArticlesType>;
  articleDetails: Record<string, GetArticleWithSubTagsResponseType>;
};
