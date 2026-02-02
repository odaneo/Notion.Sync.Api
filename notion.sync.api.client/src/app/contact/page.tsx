import { Mail, Github } from "lucide-react";
import Link from "next/link";
import type { Metadata } from "next";
import Minesweeper from "@/components/Minesweeper";

export const metadata: Metadata = {
  title: "联系我",
  alternates: { canonical: "/contact" },
  description:
    "可以通过邮件或 GitHub 与我取得联系。欢迎交流全栈开发心得、Next.js 实战问题、云原生架构设计或任何有趣的技术话题，期待与志同道合的朋友共同进步。",
  openGraph: {
    type: "website",
    siteName: "街街的脏书包",
    title: "联系我｜前沿技术与深度思考",
    description:
      "可以通过邮件或 GitHub 与我取得联系。欢迎交流全栈开发心得、Next.js 实战问题、云原生架构设计或任何有趣的技术话题，期待与志同道合的朋友共同进步。",
    url: `${process.env.HOME_URL}/contact`,
  },
  twitter: {
    card: "summary_large_image",
    title: "联系我｜前沿技术与深度思考",
    description:
      "可以通过邮件或 GitHub 与我取得联系。欢迎交流全栈开发心得、Next.js 实战问题、云原生架构设计或任何有趣的技术话题，期待与志同道合的朋友共同进步。",
  },
};

export default async function Contact() {
  const jsonLd = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "ContactPage",
        "@id": `${process.env.HOME_URL}/contact/#webpage`,
        url: `${process.env.HOME_URL}/contact`,
        name: "联系我 - 街街的脏书包",
        description:
          "可以通过邮件或 GitHub 与我取得联系。欢迎交流全栈开发心得、Next.js 实战问题、云原生架构设计或任何有趣的技术话题，期待与志同道合的朋友共同进步。",
        mainEntity: { "@id": `${process.env.HOME_URL}/#person` },
        breadcrumb: {
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
              name: "联系我",
              item: `${process.env.HOME_URL}/contact`,
            },
          ],
        },
      },
      {
        "@type": "Person",
        "@id": `${process.env.HOME_URL}/#person`,
        name: "odaneo",
        email: "mailto:odaneo@outlook.com",
        image: `${process.env.HOME_URL}/avatar.jpg`,
        sameAs: ["https://github.com/odaneo"],
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
        <h2 className="text-2xl ml-4 mb-8 italic">联系我</h2>
        <div className="flex flex-col gap-y-5 mx-10">
          <div className="flex flex-wrap">
            <Mail className="mr-5" />
            <Link href={"mailto:odaneo@outlook.com"}>odaneo@outlook.com</Link>
          </div>
          <div className="flex flex-wrap">
            <Github className="mr-5" />
            <Link target="_blank" href={"https://github.com/odaneo"}>
              github.com/odaneo
            </Link>
          </div>
        </div>
      </main>
      <div className="flex h-full w-full flex-col items-center select-none">
        <Minesweeper />
      </div>
    </>
  );
}
