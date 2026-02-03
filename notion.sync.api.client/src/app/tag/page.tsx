import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import MenuList from "@/components/tag/MenuList";
import type { Metadata } from "next";

export const revalidate = 3600;

export const metadata: Metadata = {
  title: "所有标签",
  alternates: { canonical: "/tag" },
  description:
    "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、全栈等核心标签，系统化呈现全栈开发知识体系。",
  openGraph: {
    type: "website",
    siteName: "街街的脏书包",
    title: "所有标签｜前沿技术与深度思考",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、全栈等核心标签，系统化呈现全栈开发知识体系。",
    url: `${process.env.HOME_URL}/tag`,
  },
  twitter: {
    card: "summary_large_image",
    title: "所有标签｜前沿技术与深度思考",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、全栈等核心标签，系统化呈现全栈开发知识体系。",
  },
};

export default async function Tag() {
  const { data: tagsData } = await supabase
    .rpc("get_tags_with_articles_json")
    .overrideTypes<GetTagsWithArticlesResponseType[]>();

  const tags = Array.isArray(tagsData) ? tagsData : [];

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "ItemList",
    name: "所有标签 - 街街的脏书包",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、全栈等核心标签，系统化呈现全栈开发知识体系。",
    url: `${process.env.HOME_URL}/tag`,
    numberOfItems: tags.length,
    itemListElement: tags.map((tag, index) => ({
      "@type": "ListItem",
      position: index + 1,
      url: `${process.env.HOME_URL}/tag/${tag.slug}`,
      name: tag.title,
    })),
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <h1 className="sr-only">
        街街的脏书包 - 专注于 Next.js、React、TypeScript、AWS 的全栈技术博客
      </h1>
      <main className="mt-5 w-full">
        <h2 className="text-2xl ml-4 mb-3 italic">所有标签</h2>
        {tags && <MenuList tags={tags} />}
      </main>
    </>
  );
}
