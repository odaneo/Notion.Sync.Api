import { GetTagsAndRecommendArticlesResponseType } from "@/type/api.type";
import { supabase } from "@/utils/supabase/server";
import Image from "next/image";
import MyAvatar from "../../public/avatar.jpg";
import LucideIcon from "@/components/LucideIcon";
import dynamicIconImports from "lucide-react/dynamicIconImports";
import ArticleList from "@/components/ArticleList";

export default async function AppPage() {
  const { data } = await supabase
    .rpc("get_tags_and_recommend_articles")
    .single<GetTagsAndRecommendArticlesResponseType>();

  const tags = Array.isArray(data?.Tags) ? data?.Tags : [];
  const recommendArticles = Array.isArray(data?.RecommendArticles)
    ? data?.RecommendArticles
    : [];

  return (
    <>
      <div className="mx-auto max-w-2xl flex mt-10">
        <div className="avatar ml-4">
          <div className="ring-primary ring-offset-base-100 w-24 rounded-full ring-2 ring-offset-2">
            <Image alt="avatar" width={192} height={192} src={MyAvatar} />
          </div>
        </div>
        <p className="mx-4">
          我叫 Neo，是一名旅居东京的全栈工程师，擅长
          React、AWS、C#，计划回国中。头像是我的猫，名字叫街街。
        </p>
      </div>
      <div className="my-6">
        <h3 className="text-2xl ml-4 mb-3 italic">文章分类</h3>
        <div className="flex flex-wrap flex-col sm:flex-row gap-y-2">
          {tags?.map(({ Slug, Title, LucideIconName, ArticleCount }) => {
            return (
              <div key={Slug} className="px-2 sm:w-1/3 flex justify-center">
                <div className="stats hover:shadow cursor-pointer">
                  <div className="stat">
                    <div className="stat-figure text-primary">
                      <LucideIcon
                        name={LucideIconName as keyof typeof dynamicIconImports}
                        color="var(--color-info)"
                        className="h-8 w-8"
                      />
                    </div>
                    <div className="stat-value text-lg font-medium">
                      {Title}
                    </div>
                    <div className="stat-title">{ArticleCount}篇文章</div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <div className="my-6">
        <h3 className="text-2xl ml-4 mb-3 italic">精选文章</h3>
        <div className="flex flex-col gap-y-2 mx-10">
          {recommendArticles.map(({ Id, ...props }) => {
            return (
              <div key={Id}>
                <ArticleList {...props} />
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
}
