import { supabase } from "@/utils/supabase/server";
import { GetArticleWithSubTagsResponseType } from "@/type/api.type";
import { ExtendedRecordMap } from "notion-types";
import NotionSSR from "@/components/blog/NotionSSR";
import UpdatedAtJST from "@/components/UpdatedAtJST";
import Link from "next/link";

// export const dynamic = "force-dynamic";

type PageProps = {
  params: Promise<{ tag: string; slug: string }>;
};

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

  return {
    alternates: { canonical: `/blog/${tag}/${slug}` },
    title: detail ? detail.title : null,
    description: `${detail?.title}`,
    openGraph: {
      type: "article",
      siteName: "街街的脏书包",
      title: detail?.title,
      description: `${detail?.title}`,
      url: `${process.env.HOME_URL}/blog/${tag}/${slug}`,
    },
    twitter: {
      card: "summary_large_image",
      title: detail?.title,
      description: `${detail?.title}`,
    },
  };
}

export default async function ArticlePage({ params }: PageProps) {
  const { slug } = await params;

  const detail: GetArticleWithSubTagsResponseType | null =
    await getArticleWithSubTags(slug);

  if (!detail) {
    return <p className="text-slate-500">未找到文章</p>;
  }

  return (
    <article className="overflow-x-auto mt-5">
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
        {detail.lastEditedTime && <UpdatedAtJST date={detail.lastEditedTime} />}
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
