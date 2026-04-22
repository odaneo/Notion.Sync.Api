import "server-only";
import { getCloudflareContext } from "@opennextjs/cloudflare";
import {
  ArticlesType,
  GetArticleWithSubTagsResponseType,
  GetTagsAndRecommendArticlesResponseType,
  GetTagsWithArticlesResponseType,
} from "@/type/api.type";
import {
  BlogCacheManifestType,
  TagDetailWithArticlesType,
} from "@/type/blog-cache.type";

const ACTIVE_VERSION_KEY = "blog:active";
const ACTIVE_VERSION_CACHE_TTL = 30;
const VERSIONED_DATA_CACHE_TTL = 300;

async function getBlogCache() {
  const { env } = await getCloudflareContext({ async: true });
  return env.BLOG_CACHE;
}

function getVersionedKey(version: string, name: string) {
  return `blog:v:${version}:${name}`;
}

async function getActiveVersion() {
  const cache = await getBlogCache();
  return cache.get(ACTIVE_VERSION_KEY, {
    cacheTtl: ACTIVE_VERSION_CACHE_TTL,
  });
}

async function getVersionedJson<T>(name: string) {
  const version = await getActiveVersion();

  if (!version) {
    return null;
  }

  const cache = await getBlogCache();
  const value = await cache.get(getVersionedKey(version, name), {
    cacheTtl: VERSIONED_DATA_CACHE_TTL,
  });

  return value ? (JSON.parse(value) as T) : null;
}

export async function getBlogCacheManifest() {
  return getVersionedJson<BlogCacheManifestType>("manifest");
}

export async function getTagsAndRecommendArticles() {
  return getVersionedJson<GetTagsAndRecommendArticlesResponseType>("home");
}

export async function getAllArticles() {
  return getVersionedJson<ArticlesType[]>("articles");
}

export async function getTagsWithArticles() {
  return getVersionedJson<GetTagsWithArticlesResponseType[]>("tags");
}

export async function getArticleWithSubTags(slug: string) {
  if (!slug) {
    return null;
  }

  return getVersionedJson<GetArticleWithSubTagsResponseType>(`article:${slug}`);
}

export async function getTagDetailWithArticles(tag: string) {
  if (!tag) {
    return null;
  }

  return getVersionedJson<TagDetailWithArticlesType>(`tag:${tag}`);
}
