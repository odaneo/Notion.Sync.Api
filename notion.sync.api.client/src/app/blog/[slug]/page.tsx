import { getArticleWithSubTags } from "@/lib/blog";
import { articlePath, jsonLd } from "@/lib/blog-path";
import Markdown from "@/components/blog/Markdown";
import UpdatedAtJST from "@/components/UpdatedAtJST";
import { notFound } from "next/navigation";
import Link from "next/link";

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
    <article className="mt-5 min-w-0">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: jsonLd(structuredData) }}
      />
      <h1 className="mb-3 text-2xl font-semibold tracking-tight px-4">
        {detail.title}
      </h1>
      <div className="mb-4 flex flex-wrap gap-3 px-4">
        {detail.tags?.map((tag) => (
          <span key={tag.id} className="badge badge-soft badge-info rounded">
            <Link href={`/tag/${encodeURIComponent(tag.slug)}`}>
              {tag.title}
            </Link>
          </span>
        ))}
        {detail.subTags?.map((tag) => (
          <span key={tag.id} className="badge badge-primary rounded">
            {tag.title}
          </span>
        ))}
        {detail.lastEditedTime && (
          <UpdatedAtJST date={detail.lastEditedTime} relative={false} />
        )}
      </div>
      <Markdown content={detail.content} sourcePath={detail.sourcePath} />
    </article>
  );
}
