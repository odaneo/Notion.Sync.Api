import { supabase } from "@/utils/supabase/server";
import { ArticlesType } from "@/type/api.type";
import ArticleList from "@/components/ArticleList";

export default async function Blog() {
  const { data } = await supabase
    .rpc("get_all_articles")
    .overrideTypes<ArticlesType[]>();

  const articleData = Array.isArray(data) ? data : [];
  return (
    <main className="mt-5 w-full">
      <h3 className="text-2xl ml-4 mb-5 italic">所有文章</h3>
      <div className="flex flex-col gap-y-2 mx-10">
        {articleData.map(({ id, ...props }) => {
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
