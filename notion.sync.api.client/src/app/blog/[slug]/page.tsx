import { getArticleWithSubTags } from "@/lib/blog";
import { articlePath, jsonLd } from "@/lib/blog-path";
import Markdown from "@/components/blog/Markdown";
import UpdatedAtJST from "@/components/UpdatedAtJST";
import { notFound } from "next/navigation";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";

type PageProps = { params: Promise<{ slug: string }> };
export const revalidate = false;
export function generateStaticParams() {
  return [];
}

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const detail = await getArticleWithSubTags(slug);
  if (!detail) notFound();
  return {
    alternates: { canonical: articlePath(detail.slug) },
    title: detail.title,
    description: detail.description,
    keywords: [...(detail.tags ?? []), ...(detail.subTags ?? [])].map(
      (tag) => tag.title,
    ),
    openGraph: {
      type: "article",
      siteName: "街街的脏书包",
      title: detail.title,
      description: detail.description,
      url: `${process.env.HOME_URL}${articlePath(detail.slug)}`,
    },
    twitter: {
      card: "summary_large_image",
      title: detail.title,
      description: detail.description,
    },
  };
}

export default async function ArticlePage({ params }: PageProps) {
  const { slug } = await params;
  const detail = await getArticleWithSubTags(slug);
  if (!detail) notFound();
  const structuredData = {
    "@context": "https://schema.org",
    "@type": "TechArticle",
    headline: detail.title,
    description: detail.description,
    dateModified: detail.lastEditedTime,
    author: {
      "@type": "Person",
      name: "odaneo",
      url: `${process.env.HOME_URL}/contact`,
    },
    mainEntityOfPage: {
      "@type": "WebPage",
      "@id": `${process.env.HOME_URL}${articlePath(detail.slug)}`,
    },
    keywords: [...(detail.tags ?? []), ...(detail.subTags ?? [])]
      .map((tag) => tag.title)
      .join(", "),
  };
  return (
    <article className="reading-page">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: jsonLd(structuredData) }}
      />
      <header className="article-heading">
        <Link className="text-link" href="/blog">
          <ArrowLeft size={16} aria-hidden="true" /> 所有文章
        </Link>
        <h1>{detail.title}</h1>
        <div className="entry-meta">
          {detail.lastEditedTime && (
            <UpdatedAtJST date={detail.lastEditedTime} relative={false} />
          )}
          {detail.tags?.map(({ id, slug, title }) => (
            <Link
              key={id}
              className="tag-pill"
              href={`/tag/${encodeURIComponent(slug)}`}
            >
              {title}
            </Link>
          ))}
          {detail.subTags?.map(({ id, title }) => (
            <span key={id} className="tag-pill">
              {title}
            </span>
          ))}
        </div>
      </header>
      <Markdown content={detail.content} sourcePath={detail.sourcePath} />
    </article>
  );
}
