import type {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  TagsType,
} from "@/type/api.type";
import type {
  BlogCacheManifestType,
  BlogCachePayloadType,
  TagDetailWithArticlesType,
} from "@/type/blog-cache.type";

const generatedAt = "2026-05-18T00:00:00.000Z";
const version = "local-dev";

const tags: TagsType[] = [
  {
    id: "tag-nextjs",
    slug: "nextjs",
    title: "Next.js",
    articleCount: 2,
    description: "Local mock posts about Next.js and React.",
    lucideIconName: "box",
  },
  {
    id: "tag-aws",
    slug: "aws",
    title: "AWS",
    articleCount: 1,
    description: "Local mock posts about AWS operations.",
    lucideIconName: "cloud",
  },
];

const articles: ArticlesType[] = [
  {
    id: "article-nextjs-kv-cache",
    title: "Local KV cache mock for Next.js",
    slug: "local-kv-cache-mock",
    lastEditedTime: "2026-05-18T00:00:00.000Z",
    tags: [tags[0]],
    subTags: [
      {
        id: "subtag-workers-kv",
        title: "Workers KV",
        slug: "workers-kv",
      },
    ],
  },
  {
    id: "article-react-server-components",
    title: "Reading cached blog data in server components",
    slug: "server-component-cache",
    lastEditedTime: "2026-05-17T00:00:00.000Z",
    tags: [tags[0]],
    subTags: [
      {
        id: "subtag-rsc",
        title: "RSC",
        slug: "rsc",
      },
    ],
  },
  {
    id: "article-aws-sync-job",
    title: "Publishing Notion sync results to KV",
    slug: "publish-notion-sync-to-kv",
    lastEditedTime: "2026-05-16T00:00:00.000Z",
    tags: [tags[1]],
    subTags: [
      {
        id: "subtag-hangfire",
        title: "Hangfire",
        slug: "hangfire",
      },
    ],
  },
];

const articleDetails: Record<string, GetArticleWithSubTagsResponseType> =
  Object.fromEntries(
    articles.map((article) => [
      article.slug,
      {
        ...article,
        content: "",
        description: `Mock detail for ${article.title}.`,
      },
    ]),
  );

const tagDetails: Record<string, TagDetailWithArticlesType> = Object.fromEntries(
  tags.map((tag) => [
    tag.slug,
    {
      tag,
      articles: articles.filter((article) =>
        article.tags.some((articleTag) => articleTag.id === tag.id),
      ),
    },
  ]),
);

const tagsWithArticles = tags.map((tag) => ({
  slug: tag.slug,
  title: tag.title,
  articles: tagDetails[tag.slug].articles,
}));

const payload = {
  home: {
    tags,
    recommendArticles: [articles[0], articles[2]],
  },
  articles,
  tags: tagsWithArticles,
  tagDetails,
  articleDetails,
} satisfies BlogCachePayloadType;

const manifest = {
  version,
  generatedAt,
  articleCount: articles.length,
  tagCount: tags.length,
  keys: [
    `blog:v:${version}:home`,
    `blog:v:${version}:articles`,
    `blog:v:${version}:tags`,
    ...tags.map((tag) => `blog:v:${version}:tag:${tag.slug}`),
    ...articles.map((article) => `blog:v:${version}:article:${article.slug}`),
    `blog:v:${version}:manifest`,
  ],
} satisfies BlogCacheManifestType;

export function getMockBlogCacheValue<T>(name: string) {
  if (name === "manifest") {
    return manifest as T;
  }

  if (name === "home" || name === "articles" || name === "tags") {
    return payload[name] as T;
  }

  if (name.startsWith("tag:")) {
    return (payload.tagDetails[name.slice(4)] ?? null) as T | null;
  }

  if (name.startsWith("article:")) {
    return (payload.articleDetails[name.slice(8)] ?? null) as T | null;
  }

  return null;
}
