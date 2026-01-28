import { GetTagsAndRecommendArticlesResponseType } from "@/type/api.type";
import { supabase } from "@/utils/supabase/server";
import Image from "next/image";
import MyAvatar from "../../public/avatar.jpg";
import LucideIcon from "@/components/LucideIcon";
import dynamicIconImports from "lucide-react/dynamicIconImports";
import ArticleList from "@/components/ArticleList";
import Link from "next/link";

export default async function AppPage() {
  const { data } = await supabase
    .rpc("get_tags_and_recommend_articles")
    .single<GetTagsAndRecommendArticlesResponseType>();

  const tags = Array.isArray(data?.tags) ? data?.tags : [];
  const recommendArticles = Array.isArray(data?.recommendArticles)
    ? data?.recommendArticles
    : [];

  return (
    <>
      <h1 className="sr-only">
        街街的脏书包 - 专注于 Next.js、React、TypeScript、AWS 的全栈技术博客
      </h1>
      <section className="mx-auto max-w-2xl flex mt-10">
        <div className="avatar ml-4">
          <div className="ring-primary ring-offset-base-100 w-24 rounded-full ring-2 ring-offset-2">
            <Image alt="街街的头像" width={192} height={192} src={MyAvatar} />
          </div>
        </div>
        <p className="mx-4">
          我叫 Neo，是一名旅居东京的全栈工程师，擅长
          React、AWS、C#，计划回国中。头像是我的猫，名字叫街街。
        </p>
      </section>
      <section className="my-6">
        <h2 className="text-2xl ml-4 mb-3 italic">文章分类</h2>
        <div className="flex flex-wrap flex-col sm:flex-row gap-y-2">
          {tags?.map(({ slug, title, lucideIconName, articleCount }) => {
            return (
              <div key={slug} className="px-2 sm:w-1/3 flex justify-center">
                <Link href={`/tag/${slug}`}>
                  <div className="stats hover:shadow cursor-pointer">
                    <div className="stat">
                      <div className="stat-figure text-primary">
                        <LucideIcon
                          name={
                            lucideIconName as keyof typeof dynamicIconImports
                          }
                          color="var(--color-info)"
                          className="h-8 w-8"
                        />
                      </div>
                      <h3 className="stat-value text-lg font-medium">
                        {title}
                      </h3>
                      <div className="stat-title">{articleCount}篇文章</div>
                    </div>
                  </div>
                </Link>
              </div>
            );
          })}
        </div>
      </section>
      <section className="my-6">
        <h2 className="text-2xl ml-4 mb-5 italic">精选文章</h2>
        <ul className="flex flex-col gap-y-2 mx-10">
          {recommendArticles.map(({ id, ...props }) => {
            return (
              <li key={id}>
                <ArticleList {...props} />
              </li>
            );
          })}
        </ul>
      </section>
    </>
  );
}
