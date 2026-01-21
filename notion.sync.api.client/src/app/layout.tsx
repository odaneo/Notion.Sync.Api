import type { Metadata } from "next";
import { Geist_Mono, Noto_Sans_SC } from "next/font/google";
import "./globals.css";
import ScrollToTop from "@/components/ScrollToTop";
import { SpeedInsights } from "@vercel/speed-insights/next";
import Link from "next/link";
import SeoWebsiteJsonld from "@/components/SeoWebsiteJsonld";

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
    default: "刘小可的脏书包｜前沿技术与深度思考",
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
    siteName: "刘小可的脏书包",
    title: "刘小可的脏书包｜前沿技术与深度思考",
    description: "前端、云原生、架构与性能优化的实战笔记。",
    url: `${process.env.HOME_URL}`,
  },
  twitter: {
    card: "summary_large_image",
    title: "刘小可的脏书包｜前沿技术与深度思考",
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
        className={`${notoTabs.variable} ${geistMono.variable} antialiased`}
      >
        <div>
          <header className="sticky top-0 w-full z-50 bg-white bg-white/70 backdrop-blur-md">
            <div className="navbar mx-auto max-w-7xl">
              <div className="navbar-start">
                <div className="dropdown">
                  <div
                    tabIndex={0}
                    role="button"
                    className="btn btn-ghost lg:hidden"
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      className="h-5 w-5"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M4 6h16M4 12h8m-8 6h16"
                      />
                    </svg>
                  </div>
                  <ul
                    tabIndex={-1}
                    className="menu dropdown-content bg-base-100 rounded-box z-1 mt-3 w-18 p-2 shadow"
                  >
                    <li>
                      <Link href="/" aria-label="首页">
                        首页
                      </Link>
                    </li>
                    <li>
                      <Link href="/" aria-label="博客">
                        博客
                      </Link>
                    </li>
                    <li>
                      <Link href="/" aria-label="标签">
                        标签
                      </Link>
                    </li>
                  </ul>
                </div>
                <Link
                  href="/"
                  aria-label="刘小可的脏书包｜前沿技术与深度思考"
                  className="flex items-end justify-start gap-4 h-10"
                >
                  <h1 className="text-3xl font-bold text-gray-800 leading-tight min-w-56">
                    刘小可的脏书包
                  </h1>
                  <p className="sr-only">
                    关键词：Next.js、React、TypeScript、DevOps、Databricks、AWS、架构与性能优化、前沿技术与深度思考
                  </p>
                </Link>
              </div>
              <div className="navbar-center hidden lg:flex">
                <ul className="menu menu-horizontal px-1">
                  <li>
                    <Link
                      href="/"
                      aria-label="首页"
                      className="hover:!bg-transparent hover:underline hover:underline-offset-4 transition-all"
                    >
                      首页
                    </Link>
                  </li>
                  <li>
                    <Link
                      href="/"
                      aria-label="博客"
                      className="hover:!bg-transparent hover:underline hover:underline-offset-4 transition-all"
                    >
                      博客
                    </Link>
                  </li>
                  <li>
                    <Link
                      href="/"
                      aria-label="标签"
                      className="hover:!bg-transparent hover:underline hover:underline-offset-4 transition-all"
                    >
                      标签
                    </Link>
                  </li>
                </ul>
              </div>
            </div>
          </header>
          {children}
          <ScrollToTop />
          <SpeedInsights />
          <SeoWebsiteJsonld />
        </div>
      </body>
    </html>
  );
}
