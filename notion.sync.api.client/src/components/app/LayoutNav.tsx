"use client";
import { usePathname } from "next/navigation";
import { LayoutNavConst } from "@/const/LayoutNavConst";
import Link from "next/link";

export default function LayoutNav() {
  const section = "/" + (usePathname()?.split("/")[1] ?? "");
  return (
    <nav className="site-nav" aria-label="主导航">
      {LayoutNavConst.map(({ name, key, href }) => (
        <Link
          key={key}
          href={href}
          aria-current={section === href ? "page" : undefined}
        >
          {name}
        </Link>
      ))}
    </nav>
  );
}
