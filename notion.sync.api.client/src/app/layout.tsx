import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import ScrollToTop from "@/components/ScrollToTop";
import { SpeedInsights } from "@vercel/speed-insights/next";
import Link from "next/link";
import SeoWebsiteJsonld from "@/components/SeoWebsiteJsonld";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  metadataBase: new URL(`${process.env.HOME_URL}`),
  title: {
    default: "脏书包｜前沿技术与深度思考",
    template: "%s - 脏书包",
  },
  description:
    "聚焦前沿技术、前端工程与云原生实践，记录可落地的经验与深度思考。面向工程师与技术爱好者：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化。",
  keywords: [
    "前沿技术",
    "前端工程",
    "云原生",
    "Next.js",
    "React",
    "TypeScript",
    "DevOps",
    "Databricks",
    "AWS",
    "架构",
    "性能优化",
    "技术博客",
  ],
  alternates: { canonical: "/" },
  openGraph: {
    type: "website",
    siteName: "脏书包",
    title: "脏书包｜前沿技术与深度思考",
    description: "前端、云原生、架构与性能优化的实战笔记。",
    url: `${process.env.HOME_URL}`,
  },
  twitter: {
    card: "summary_large_image",
    title: "脏书包｜前沿技术与深度思考",
    description: "前端、云原生、架构与性能优化的实战笔记。",
  },
  robots: { index: true, follow: true },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html data-theme="wireframe" lang="zh-CN">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <header className="flex items-center gap-4 mx-auto max-w-7xl p-6">
          <Link href="/" aria-label="脏书包">
            <h1 className="text-3xl font-bold text-gray-800 leading-tight">
              脏书包
            </h1>
            <h2 className="text-gray-500 text-base mt-1">
              前沿技术 · 前端工程 · 云原生实践与深度思考
            </h2>
            <p className="sr-only">
              关键词：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化
            </p>
          </Link>
        </header>
        {children}
        <ScrollToTop />
        <SpeedInsights />
        <SeoWebsiteJsonld />
      </body>
    </html>
  );
}
