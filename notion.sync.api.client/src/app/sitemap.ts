import type { MetadataRoute } from "next";
import { getAllArticles, getTagsWithArticles } from "@/lib/blog";
import { articlePath } from "@/lib/blog-path";

export const revalidate = false;

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [articles, tags] = await Promise.all([
    getAllArticles(),
    getTagsWithArticles(),
  ]);
  const base = process.env.HOME_URL;
  return [
    ...["", "/blog", "/tag", "/contact"].map((path) => ({
      url: `${base}${path}`,
    })),
    ...tags.map((tag) => ({
      url: `${base}/tag/${encodeURIComponent(tag.slug)}`,
    })),
    ...articles.map((article) => ({
      url: `${base}${articlePath(article.slug)}`,
      lastModified: article.lastEditedTime,
    })),
  ];
}
