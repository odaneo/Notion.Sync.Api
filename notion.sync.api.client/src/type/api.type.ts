export type ArticlesType = {
  Id: string;
  Title: string;
  Slug: string;
  LastEditedTime: string;
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

export type GetArticleWithSubTagsResponseType = {
  Content: string;
  Title: string;
  Slug: string;
  LastEditedTime: string;
  Id: string;
  SubTags?: SubTagsType[];
};
