import { supabase } from "@/utils/supabase/server";
import { ArticlesType, TagsType } from "@/type/api.type";
import ArticleList from "@/components/ArticleList";

type PageProps = {
  params: Promise<{ tag: string }>;
};

export default async function TagPage({ params }: PageProps) {
  const { tag } = await params;

  const { data, error } = await supabase
    .rpc("get_tag_detail_with_articles", { tag_slug: tag })
    .single<{ tag: TagsType; articles: ArticlesType[] }>();

  if (error || !data) {
    return null;
  }

  const article = Array.isArray(data?.articles) ? data?.articles : [];

  return (
    <main className="mt-5 w-full">
      <h3 className="text-2xl ml-4 mb-5 italic">标签：{data.tag?.title}</h3>
      <div className="flex flex-col gap-y-2 mx-10">
        {article.map(({ id, ...props }) => {
          return (
            <div key={id}>
              <ArticleList {...props} />
            </div>
          );
        })}
      </div>
    </main>
  );
}
