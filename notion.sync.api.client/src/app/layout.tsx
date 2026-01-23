import type { Metadata } from "next";
import { Geist_Mono, Noto_Sans_SC } from "next/font/google";
import "./globals.css";
import ScrollToTop from "@/components/app/ScrollToTop";
import { SpeedInsights } from "@vercel/speed-insights/next";
import Link from "next/link";
import SeoWebsiteJsonld from "@/components/app/SeoWebsiteJsonld";
import LayoutNav from "@/components/app/LayoutNav";
import { LayoutNavConst } from "@/const/LayoutNavConst";
import { Menu, Github } from "lucide-react";

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
    default: "Neo的脏书包｜前沿技术与深度思考",
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
    siteName: "Neo的脏书包",
    title: "Neo的脏书包｜前沿技术与深度思考",
    description: "前端、云原生、架构与性能优化的实战笔记。",
    url: `${process.env.HOME_URL}`,
  },
  twitter: {
    card: "summary_large_image",
    title: "Neo的脏书包｜前沿技术与深度思考",
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
    <html
      data-theme="wireframe"
      lang="zh-CN"
      style={{ scrollbarGutter: "stable" }}
    >
      <body
        className={`${notoTabs.variable} ${geistMono.variable} antialiased min-h-screen flex flex-col`}
      >
        <div className="mx-auto max-w-4xl flex-1 w-full">
          <header className="sticky top-0 w-full z-50 backdrop-blur-xs">
            <div className="navbar">
              <div className="navbar-start">
                <div className="dropdown">
                  <div
                    tabIndex={0}
                    role="button"
                    className="btn btn-ghost sm:hidden"
                  >
                    <Menu />
                  </div>
                  <ul
                    tabIndex={-1}
                    className="menu dropdown-content bg-base-100 rounded-box z-1 mt-3 w-18 p-2 shadow"
                  >
                    {LayoutNavConst.map(({ key, name, href }) => {
                      return (
                        <li key={key}>
                          <Link href={href} aria-label={name}>
                            {name}
                          </Link>
                        </li>
                      );
                    })}
                  </ul>
                </div>
                <Link
                  href="/"
                  aria-label="Neo的脏书包｜前沿技术与深度思考"
                  className="flex items-end justify-start gap-4 h-10"
                >
                  <h1 className="text-3xl font-bold text-gray-800 leading-tight min-w-56">
                    Neo的脏书包
                  </h1>
                  <p className="sr-only">
                    关键词：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化、前沿技术与深度思考
                  </p>
                </Link>
              </div>
              <div className="navbar-center hidden sm:flex">
                <LayoutNav />
              </div>
              <div className="navbar-end">
                <Link
                  href={"https://github.com/odaneo"}
                  target="_blank"
                  className="btn btn-ghost btn-circle"
                >
                  <Github />
                </Link>
              </div>
            </div>
          </header>
          {children}
        </div>
        <footer className="mx-auto max-w-4xl text-base-content p-6 w-full">
          <div className="footer sm:footer-horizontal">
            <nav>
              <Link
                className="link link-hover hover:underline-offset-3"
                href={"/contact"}
              >
                联系
              </Link>
            </nav>
          </div>
          <aside className="mt-10 text-center">
            <p>
              Copyright © {new Date().getFullYear()} -
              <Link
                className="link link-hover hover:underline-offset-3"
                href={"https://odaneo.com"}
              >
                {" "}
                Neo.{" "}
              </Link>
              保留所有权利
            </p>
          </aside>
        </footer>
        <ScrollToTop />
        <SpeedInsights />
        <SeoWebsiteJsonld />
      </body>
    </html>
  );
}
