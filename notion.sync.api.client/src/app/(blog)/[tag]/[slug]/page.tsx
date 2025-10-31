import { supabase } from "@/utils/supabase/server";
import { GetArticleWithSubTagsResponseType } from "@/type/api.type";
import { ExtendedRecordMap } from "notion-types";
import NotionSSR from "@/components/NotionSSR";
import UpdatedAtJST from "@/components/UpdatedAtJST";

export const dynamic = "force-dynamic";

type PageProps = {
  params: Promise<{ tag: string; slug: string }>;
};

export default async function ArticlePage({ params }: PageProps) {
  const { slug } = await params;

  let detail: GetArticleWithSubTagsResponseType | null = null;
  if (slug) {
    const { data } = await supabase
      .rpc("get_article_by_slug", { article_slug: slug })
      .single()
      .overrideTypes<GetArticleWithSubTagsResponseType>();
    detail = data ?? null;
  }
  if (!detail) {
    return <p className="text-slate-500">未找到文章</p>;
  }

  return (
    <article>
      <h2 className="mb-3 text-2xl font-semibold tracking-tight">
        {detail.Title}
      </h2>

      <div className="mb-4 flex flex-wrap gap-3">
        {detail.SubTags?.map((t) => (
          <span key={t.Id} className="badge badge-primary">
            {t.Title}
          </span>
        ))}
        {detail.LastEditedTime && <UpdatedAtJST date={detail.LastEditedTime} />}
      </div>

      {detail.Content ? (
        <div>
          <NotionSSR
            recordMap={JSON.parse(detail.Content) as ExtendedRecordMap}
          />
        </div>
      ) : (
        <p className="text-slate-500">暂无内容</p>
      )}
    </article>
  );
}
