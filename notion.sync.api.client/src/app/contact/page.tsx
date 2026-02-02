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
  return (
    <>
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
