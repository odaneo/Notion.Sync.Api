import { jsonLd as serializeJsonLd } from "@/lib/blog-path";
import { getTagsWithArticles } from "@/lib/blog";
import MenuList from "@/components/tag/MenuList";
import type { Metadata } from "next";

export const revalidate = false;

export const metadata: Metadata = {
  title: "所有标签",
  alternates: { canonical: "/tag" },
  description:
    "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、C#等核心标签，系统化呈现全栈开发知识体系。",
  openGraph: {
    type: "website",
    siteName: "街街的脏书包",
    title: "所有标签｜前沿技术与深度思考",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、C#等核心标签，系统化呈现全栈开发知识体系。",
    url: `${process.env.HOME_URL}/tag`,
  },
  twitter: {
    card: "summary_large_image",
    title: "所有标签｜前沿技术与深度思考",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、C#等核心标签，系统化呈现全栈开发知识体系。",
  },
};

export default async function Tag() {
  const tagsData = await getTagsWithArticles();

  const tags = Array.isArray(tagsData) ? tagsData : [];

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "ItemList",
    name: "所有标签 - 街街的脏书包",
    description:
      "探索街街的脏书包的所有技术文章分类：包含 AWS、Git、日本职场、前端、C#等核心标签，系统化呈现全栈开发知识体系。",
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
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(jsonLd) }}
      />
      <header className="page-heading">
        <p className="eyebrow">共 {tags.length} 个主题</p>
        <h1>标签</h1>
        <p>按主题浏览文章。</p>
      </header>
      <MenuList tags={tags} />
    </>
  );
}
