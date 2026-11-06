import { ArrowUpRight, Mail, Github } from "lucide-react";
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
      <header className="page-heading">
        <p className="eyebrow">Curtis</p>
        <h1>联系我</h1>
        <p>欢迎交流技术问题，也欢迎聊聊你最近在做的项目。</p>
      </header>
      <div className="contact-links">
        <a href="mailto:odaneo@outlook.com">
          <Mail size={19} aria-hidden="true" />
          <span>odaneo@outlook.com</span>
          <ArrowUpRight size={17} aria-hidden="true" />
        </a>
        <a
          href="https://github.com/odaneo"
          target="_blank"
          rel="noopener noreferrer"
        >
          <Github size={19} aria-hidden="true" />
          <span>github.com/odaneo</span>
          <ArrowUpRight size={17} aria-hidden="true" />
        </a>
      </div>
      <section className="game-section" aria-labelledby="game-heading">
        <h2 id="game-heading">扫雷</h2>
        <div className="flex min-w-0 flex-col items-center select-none">
          <Minesweeper />
        </div>
      </section>
    </>
  );
}
