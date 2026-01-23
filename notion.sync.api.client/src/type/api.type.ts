export type ArticlesType = {
  Id: string;
  Title: string;
  Slug: string;
  LastEditedTime: string;
  Tags: TagsType[];
  SubTags?: SubTagsType[];
};

export type GetTagsWithArticlesResponseType = {
  Slug: string;
  Title: string;
  Articles: ArticlesType[];
};

export type SubTagsType = {
  Id: string;
  Title: string;
  Slug: string;
};

export type TagsType = {
  Id: string;
  Slug: string;
  Title: string;
  ArticleCount: number;
  LucideIconName: string;
};

export type GetArticleWithSubTagsResponseType = {
  Content: string;
  Title: string;
  Slug: string;
  LastEditedTime: string;
  Id: string;
  SubTags?: SubTagsType[];
};

export type GetTagsAndRecommendArticlesResponseType = {
  Tags: TagsType[];
  RecommendArticles: ArticlesType[];
};
