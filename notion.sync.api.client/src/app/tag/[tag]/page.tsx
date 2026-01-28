import { supabase } from "@/utils/supabase/server";
import { ArticlesType, TagsType } from "@/type/api.type";
import ArticleList from "@/components/ArticleList";

type PageProps = {
  params: Promise<{ tag: string }>;
};

async function getTagDetailWithArticles(tag: string) {
  if (tag) {
    const { data } = await supabase
      .rpc("get_tag_detail_with_articles", { tag_slug: tag })
      .single<{ tag: TagsType; articles: ArticlesType[] }>();
    return data ?? null;
  }
  return null;
}

export async function generateMetadata({ params }: PageProps) {
  const { tag } = await params;

  const data = await getTagDetailWithArticles(tag);

  return {
    title: `标签：${data?.tag?.title}`,
    alternates: { canonical: `/tag/${tag}` },
    description: `浏览关于${data?.tag?.title}的深度技术文章列表。`,
    openGraph: {
      type: "website",
      siteName: "街街的脏书包",
      title: `标签：${data?.tag?.title}｜前沿技术与深度思考`,
      description: `浏览关于${data?.tag?.title}的深度技术文章列表。`,
      url: `${process.env.HOME_URL}/tag/${tag}`,
    },
    twitter: {
      card: "summary_large_image",
      title: `标签：${data?.tag?.title}｜前沿技术与深度思考`,
      description: `浏览关于${data?.tag?.title}的深度技术文章列表。`,
    },
  };
}

export default async function TagPage({ params }: PageProps) {
  const { tag } = await params;

  const data = await getTagDetailWithArticles(tag);

  if (!data) {
    return null;
  }

  const article = Array.isArray(data?.articles) ? data?.articles : [];

  return (
    <>
      <h1 className="sr-only">
        街街的脏书包 - 专注于 Next.js、React、TypeScript、AWS 的全栈技术博客
      </h1>
      <main className="mt-5 w-full">
        <h2 className="text-2xl ml-4 mb-5 italic">标签：{data.tag?.title}</h2>
        <ul className="flex flex-col gap-y-2 mx-10">
          {article.map(({ id, ...props }) => {
            return (
              <li key={id}>
                <ArticleList {...props} />
              </li>
            );
          })}
        </ul>
      </main>
    </>
  );
}
