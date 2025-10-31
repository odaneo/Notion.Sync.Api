import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import ScrollToTop from "@/components/ScrollToTop";
import { SpeedInsights } from "@vercel/speed-insights/next";
import Link from "next/link";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "脏书包",
  description: "分享前沿技术与深度思考",
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
            <p className="text-gray-500 text-base mt-1">
              分享前沿技术与深度思考
            </p>
          </Link>
        </header>
        {children}
        <ScrollToTop />
        <SpeedInsights />
      </body>
    </html>
  );
}
