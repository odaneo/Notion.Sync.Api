import type { Metadata } from "next";
import { Geist_Mono, Noto_Sans_SC } from "next/font/google";
import "./globals.css";
import ScrollToTop from "@/components/app/ScrollToTop";
import Link from "next/link";
import SeoWebsiteJsonld from "@/components/app/SeoWebsiteJsonld";
import LayoutNav from "@/components/app/LayoutNav";
import { Github, PawPrint } from "lucide-react";

const notoTabs = Noto_Sans_SC({
  subsets: ["latin"],
  display: "swap",
  weight: ["400", "500", "700"],
  variable: "--font-noto-sans",
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  metadataBase: new URL(`${process.env.HOME_URL}`),
  title: {
    default: "街街的脏书包｜前沿技术与深度思考",
    template: "%s - 街街的脏书包",
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
    siteName: "街街的脏书包",
    title: "街街的脏书包｜前沿技术与深度思考",
    description:
      "聚焦前沿技术、前端工程与云原生实践，记录可落地的经验与深度思考。面向工程师与技术爱好者：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化。",
    url: `${process.env.HOME_URL}`,
  },
  twitter: {
    card: "summary_large_image",
    title: "街街的脏书包｜前沿技术与深度思考",
    description:
      "聚焦前沿技术、前端工程与云原生实践，记录可落地的经验与深度思考。面向工程师与技术爱好者：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化。",
  },
  robots: { index: true, follow: true },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      data-theme="wireframe"
      lang="zh-CN"
      style={{ scrollbarGutter: "stable" }}
    >
      <body
        className={`${notoTabs.variable} ${geistMono.variable} antialiased min-h-screen flex flex-col`}
      >
        <SeoWebsiteJsonld />
        <header className="site-header">
          <div className="site-shell site-header-inner">
            <Link href="/" className="site-brand">
              <span className="site-mark">
                <PawPrint size={19} aria-hidden="true" />
              </span>
              街街的脏书包
            </Link>
            <LayoutNav />
            <a
              href="https://github.com/odaneo"
              target="_blank"
              rel="noopener noreferrer"
              className="icon-link site-github"
              aria-label="GitHub"
            >
              <Github size={19} aria-hidden="true" />
            </a>
          </div>
        </header>
        <main className="site-shell site-main">{children}</main>
        <footer className="site-shell site-footer">
          <p>© {new Date().getFullYear()} Curtis · 街街的脏书包</p>
          <Link href="/contact">联系我</Link>
        </footer>
        <ScrollToTop />
      </body>
    </html>
  );
}
