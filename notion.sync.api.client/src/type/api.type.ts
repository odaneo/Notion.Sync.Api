export type ArticlesType = {
  id: string;
  title: string;
  slug: string;
  lastEditedTime: string;
  tags: TagsType[];
  subTags?: SubTagsType[];
};

export type GetTagsWithArticlesResponseType = {
  slug: string;
  title: string;
  articles: ArticlesType[];
};

export type SubTagsType = {
  id: string;
  title: string;
  slug: string;
};

export type TagsType = {
  id: string;
  slug: string;
  title: string;
  articleCount: number;
  lucideIconName: string;
};

export type GetArticleWithSubTagsResponseType = {
  content: string;
  title: string;
  slug: string;
  lastEditedTime: string;
  id: string;
  subTags?: SubTagsType[];
};

export type GetTagsAndRecommendArticlesResponseType = {
  tags: TagsType[];
  recommendArticles: ArticlesType[];
};
