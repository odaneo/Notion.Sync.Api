import { supabase } from "@/utils/supabase/server";
import { GetArticleWithSubTagsResponseType } from "@/type/api.type";
import { ExtendedRecordMap } from "notion-types";
import NotionSSR from "@/components/blog/NotionSSR";
import UpdatedAtJST from "@/components/UpdatedAtJST";

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
      <h2 className="mb-3 text-2xl font-semibold tracking-tight px-4">
        {detail.title}
      </h2>

      <div className="mb-4 flex flex-wrap gap-3 px-4">
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
