import { supabase } from "@/utils/supabase/server";
import { ArticlesType } from "@/type/api.type";
import ArticleList from "@/components/ArticleList";
import type { Metadata } from "next";

export const revalidate = 3600;

export const metadata: Metadata = {
  title: "最新技术分享",
  alternates: { canonical: "/blog" },
  description: `精选全栈开发技术文章：涵盖 Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。`,
  openGraph: {
    type: "website",
    siteName: "街街的脏书包",
    title: "最新技术分享｜前沿技术与深度思考",
    description:
      "精选全栈开发技术文章：涵盖 Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
    url: `${process.env.HOME_URL}/blog`,
  },
  twitter: {
    card: "summary_large_image",
    title: "最新技术分享｜前沿技术与深度思考",
    description:
      "精选全栈开发技术文章：涵盖 Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
  },
};

export default async function Blog() {
  const { data } = await supabase
    .rpc("get_all_articles")
    .overrideTypes<ArticlesType[]>();

  const articleData = Array.isArray(data) ? data : [];

  const jsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "CollectionPage",
        "@id": `${process.env.HOME_URL}/blog/#collection`,
        url: `${process.env.HOME_URL}/blog`,
        name: "最新技术分享 - 街街的脏书包",
        description:
          "精选全栈开发技术文章：涵盖 Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
        mainEntity: {
          "@type": "ItemList",
          itemListElement: articleData.map((article, index) => ({
            "@type": "ListItem",
            position: index + 1,
            url: `${process.env.HOME_URL}/blog/${article.tags[0].slug}/${article.slug}`,
            name: article.title,
          })),
        },
      },
      {
        "@type": "BreadcrumbList",
        itemListElement: [
          {
            "@type": "ListItem",
            position: 1,
            name: "首页",
            item: process.env.HOME_URL,
          },
          {
            "@type": "ListItem",
            position: 2,
            name: "最新技术分享",
            item: `${process.env.HOME_URL}/blog`,
          },
        ],
      },
    ],
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
        <h2 className="text-2xl ml-4 mb-5 italic">最新技术分享</h2>
        <ul className="flex flex-col gap-y-2 mx-10">
          {articleData.map(({ id, ...props }) => {
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
