import { supabase } from "@/utils/supabase/server";
import { GetArticleWithSubTagsResponseType } from "@/type/api.type";

export const dynamic = "force-dynamic";

type PageProps = {
  params: Promise<{ tag: string; slug: string; id: string }>;
};

export default async function ArticlePage({ params }: PageProps) {
  const { id } = await params;

  let detail: GetArticleWithSubTagsResponseType | null = null;
  if (id) {
    const { data } = await supabase
      .rpc("get_article_with_subtags", { article_id: id })
      .single()
      .overrideTypes<GetArticleWithSubTagsResponseType>();
    detail = data ?? null;
  }
  if (!detail) {
    return <div className="text-slate-600">未找到文章</div>;
  }

  return (
    <article className="prose prose-slate max-w-none">
      <h2 className="mb-2 text-2xl font-semibold tracking-tight">
        {detail.Title}
      </h2>
      {detail.SubTags?.length ? (
        <div className="mb-4 flex flex-wrap gap-2">
          {detail.SubTags.map((t) => (
            <span
              key={t.Id}
              className="rounded-full border border-slate-200 px-2.5 py-0.5 text-xs text-slate-600"
            >
              {t.Title}
            </span>
          ))}
        </div>
      ) : null}
      {detail.Content ? (
        <div
          className="leading-7 text-slate-700"
          dangerouslySetInnerHTML={{ __html: detail.Content }}
        />
      ) : (
        <p className="text-slate-600">暂无内容</p>
      )}
    </article>
  );
}
