import { articlePath, jsonLd } from "@/lib/blog-path";
import { getTagsAndRecommendArticles } from "@/lib/blog";
import Image from "next/image";
import ArticleList from "@/components/ArticleList";
import Link from "next/link";
import { ArrowRight, ArrowUpRight } from "lucide-react";

export const revalidate = false;

export default async function AppPage() {
  const data = await getTagsAndRecommendArticles();

  const tags = Array.isArray(data?.tags) ? data?.tags : [];
  const recommendArticles = Array.isArray(data?.recommendArticles)
    ? data?.recommendArticles
    : [];

  const homeJsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "WebSite",
        "@id": `${process.env.HOME_URL}/#website`,
        url: process.env.HOME_URL,
        name: "街街的脏书包",
        description:
          "聚焦前沿技术、前端工程与云原生实践，记录可落地的经验与深度思考。面向工程师与技术爱好者：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化。",
        publisher: { "@id": `${process.env.HOME_URL}/#person` },
      },
      {
        "@type": "Person",
        "@id": `${process.env.HOME_URL}/#person`,
        name: "odaneo",
        jobTitle: "Full Stack Engineer",
        url: process.env.HOME_URL,
        image: `${process.env.HOME_URL}/avatar.jpg`,
        description: "一名全栈工程师，擅长 React、AWS、C#。",
        sameAs: ["https://github.com/odaneo"],
      },
      {
        "@type": "CollectionPage",
        "@id": `${process.env.HOME_URL}/#collection`,
        url: process.env.HOME_URL,
        name: "街街的脏书包 - 精选文章",
        mainEntity: {
          "@type": "ItemList",
          itemListElement: recommendArticles.map((article, index) => ({
            "@type": "ListItem",
            position: index + 1,
            url: `${process.env.HOME_URL}${articlePath(article.slug)}`,
            name: article.title,
          })),
        },
      },
    ],
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: jsonLd(homeJsonLd) }}
      />
      <section className="home-hero">
        <div>
          <h1>街街的脏书包</h1>
          <p className="hero-description">
            我是 Curtis，一名全栈开发者。这里记录 React、.NET 和 AWS
            的实践，也收下平时遇到的有趣问题。
          </p>
          <Link className="primary-link" href="/blog">
            浏览文章 <ArrowRight size={17} aria-hidden="true" />
          </Link>
        </div>
        <figure className="hero-photo">
          <Image
            alt="橘猫街街"
            width={224}
            height={224}
            src="/avatar.jpg"
            priority
          />
        </figure>
      </section>
      <div className="home-sections">
        <section aria-labelledby="featured-heading">
          <div className="section-heading">
            <h2 id="featured-heading">精选文章</h2>
            <Link className="text-link" href="/blog">
              查看全部 <ArrowUpRight size={15} aria-hidden="true" />
            </Link>
          </div>
          <ul className="entry-list">
            {recommendArticles.map(({ id, ...props }) => (
              <li key={id}>
                <ArticleList {...props} />
              </li>
            ))}
          </ul>
        </section>
        <section className="home-topics" aria-labelledby="topics-heading">
          <div className="section-heading">
            <h2 id="topics-heading">文章分类</h2>
          </div>
          <ul className="topic-list">
            {tags.map(({ slug, title, articleCount }) => (
              <li key={slug}>
                <Link href={`/tag/${encodeURIComponent(slug)}`}>
                  <span>{title}</span>
                  <span className="entry-date">{articleCount} 篇</span>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </>
  );
}
