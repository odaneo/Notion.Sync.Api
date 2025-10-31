"use client";
import { usePathname } from "next/navigation";
import Link from "next/link";

export default function MenuList({
  href,
  title,
  slug,
}: {
  href: string;
  title: string;
  slug: string;
}) {
  const pathname = usePathname();

  const isActive = pathname?.split("/")[2] === slug;

  return (
    <li>
      <Link className={isActive ? "menu-active" : ""} href={href}>
        {title}
      </Link>
    </li>
  );
}
