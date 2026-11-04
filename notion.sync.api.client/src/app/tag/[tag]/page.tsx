import { articlePath, jsonLd as serializeJsonLd } from "@/lib/blog-path";
import { getTagDetailWithArticles } from "@/lib/blog";
import ArticleList from "@/components/ArticleList";
import { notFound } from "next/navigation";

type PageProps = {
  params: Promise<{ tag: string }>;
};

export const revalidate = false;
export function generateStaticParams() {
  return [];
}

export async function generateMetadata({ params }: PageProps) {
  const { tag } = await params;

  const data = await getTagDetailWithArticles(tag);
  if (!data) notFound();

  return {
    title: `标签：${data?.tag?.title}`,
    alternates: { canonical: `/tag/${tag}` },
    description: `${data?.tag?.description}`,
    openGraph: {
      type: "website",
      siteName: "街街的脏书包",
      title: `标签：${data?.tag?.title}｜前沿技术与深度思考`,
      description: `${data?.tag?.description}`,
      url: `${process.env.HOME_URL}/tag/${tag}`,
    },
    twitter: {
      card: "summary_large_image",
      title: `标签：${data?.tag?.title}｜前沿技术与深度思考`,
      description: `${data?.tag?.description}`,
    },
  };
}

export default async function TagPage({ params }: PageProps) {
  const { tag } = await params;

  const data = await getTagDetailWithArticles(tag);

  if (!data) {
    notFound();
  }

  const article = Array.isArray(data?.articles) ? data?.articles : [];

  const jsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "CollectionPage",
        name: `标签：${data.tag?.title} - 街街的脏书包`,
        description: data.tag?.description,
        url: `${process.env.HOME_URL}/tag/${tag}`,
        mainEntity: {
          "@type": "ItemList",
          itemListElement: article.map((post, index) => ({
            "@type": "ListItem",
            position: index + 1,
            url: `${process.env.HOME_URL}${articlePath(post.slug)}`,
            name: post.title,
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
            name: "标签",
            item: `${process.env.HOME_URL}/tag`,
          },
          {
            "@type": "ListItem",
            position: 3,
            name: data.tag?.title,
            item: `${process.env.HOME_URL}/tag/${tag}`,
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
      <h1 className="sr-only">
        街街的脏书包 - 专注于 C#、Next.js、React、TypeScript、AWS 的全栈技术博客
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
