import { articlePath, jsonLd as serializeJsonLd } from "@/lib/blog-path";
import { getAllArticles } from "@/lib/blog";
import ArticleList from "@/components/ArticleList";
import type { Metadata } from "next";

export const revalidate = false;

export const metadata: Metadata = {
  title: "最新技术分享",
  alternates: { canonical: "/blog" },
  description: `精选全栈开发技术文章：涵盖C#、Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。`,
  openGraph: {
    type: "website",
    siteName: "街街的脏书包",
    title: "最新技术分享｜前沿技术与深度思考",
    description:
      "精选全栈开发技术文章：涵盖C#、Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
    url: `${process.env.HOME_URL}/blog`,
  },
  twitter: {
    card: "summary_large_image",
    title: "最新技术分享｜前沿技术与深度思考",
    description:
      "精选全栈开发技术文章：涵盖C#、Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
  },
};

export default async function Blog() {
  const data = await getAllArticles();

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
          "精选全栈开发技术文章：涵盖C#、Next.js、React、TypeScript 核心原理，AWS 云架构实战，以及作者对技术趋势的深度思考。助你构建高性能、可扩展的现代 Web 应用。",
        mainEntity: {
          "@type": "ItemList",
          itemListElement: articleData.map((article, index) => ({
            "@type": "ListItem",
            position: index + 1,
            url: `${process.env.HOME_URL}${articlePath(article.slug)}`,
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
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(jsonLd) }}
      />
      <header className="page-heading">
        <p className="eyebrow">共 {articleData.length} 篇文章</p>
        <h1>文章</h1>
        <p>关于前端、后端与云架构的实践笔记。</p>
      </header>
      <ul className="entry-list">
        {articleData.map(({ id, ...props }) => (
          <li key={id}>
            <ArticleList {...props} />
          </li>
        ))}
      </ul>
    </>
  );
}
