import { supabase } from "@/utils/supabase/server";
import { GetArticleWithSubTagsResponseType } from "@/type/api.type";
import { ExtendedRecordMap } from "notion-types";
import NotionSSR from "@/components/blog/NotionSSR";
import UpdatedAtJST from "@/components/UpdatedAtJST";
import Link from "next/link";

type PageProps = {
  params: Promise<{ tag: string; slug: string }>;
};

export const dynamicParams = true;
// export const dynamic = "force-static";
export const revalidate = 3600;
export async function generateStaticParams() {
  return [];
}

async function getArticleWithSubTags(slug: string) {
  if (slug) {
    const { data } = await supabase
      .rpc("get_article_by_slug", { article_slug: slug })
      .single()
      .overrideTypes<GetArticleWithSubTagsResponseType>();
    return data ?? null;
  }
  return null;
}

export async function generateMetadata({ params }: PageProps) {
  const { tag, slug } = await params;

  const detail: GetArticleWithSubTagsResponseType | null =
    await getArticleWithSubTags(slug);

  const allKeywords = [...(detail?.tags || []), ...(detail?.subTags || [])]
    .map((t) => t.title)
    .join(", ");

  return {
    alternates: { canonical: `/blog/${tag}/${slug}` },
    title: detail ? detail.title : null,
    description: `${detail?.description}`,
    keywords: allKeywords,
    openGraph: {
      type: "article",
      siteName: "街街的脏书包",
      title: detail?.title,
      description: `${detail?.description}`,
      url: `${process.env.HOME_URL}/blog/${tag}/${slug}`,
    },
    twitter: {
      card: "summary_large_image",
      title: detail?.title,
      description: `${detail?.description}`,
    },
  };
}

export default async function ArticlePage({ params }: PageProps) {
  const { tag, slug } = await params;

  const detail: GetArticleWithSubTagsResponseType | null =
    await getArticleWithSubTags(slug);

  if (!detail) {
    return <p className="text-slate-500">未找到文章</p>;
  }

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "TechArticle",
    headline: detail.title,
    description: detail.description,
    datePublished: detail.lastEditedTime,
    dateModified: detail.lastEditedTime,
    author: {
      "@type": "Person",
      name: "odaneo",
      url: `${process.env.HOME_URL}/contact`,
    },
    publisher: {
      "@type": "Organization",
      name: "街街的脏书包",
      logo: {
        "@type": "ImageObject",
        url: `${process.env.HOME_URL}/favicon.ico`,
      },
    },
    mainEntityOfPage: {
      "@type": "WebPage",
      "@id": `${process.env.HOME_URL}/blog/${tag}/${slug}`,
    },
    keywords: [...(detail.tags || []), ...(detail.subTags || [])]
      .map((t) => t.title)
      .join(", "),
  };

  return (
    <article className="overflow-x-auto mt-5">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <h1 className="mb-3 text-2xl font-semibold tracking-tight px-4">
        {detail.title}
      </h1>

      <div className="mb-4 flex flex-wrap gap-3 px-4">
        {detail.tags?.map((t) => (
          <span key={t.id} className="badge badge-soft badge-info rounded">
            <Link key={t.id} href={`/tag/${t.slug}`}>
              {t.title}
            </Link>
          </span>
        ))}
        {detail.subTags?.map((t) => (
          <span key={t.id} className="badge badge-primary rounded">
            {t.title}
          </span>
        ))}
        {detail.lastEditedTime && (
          <UpdatedAtJST date={detail.lastEditedTime} relative={false} />
        )}
      </div>

      {detail.content ? (
        <div>
          <NotionSSR
            recordMap={JSON.parse(detail.content) as ExtendedRecordMap}
          />
        </div>
      ) : (
        <p className="text-slate-500">暂无内容</p>
      )}
    </article>
  );
}
