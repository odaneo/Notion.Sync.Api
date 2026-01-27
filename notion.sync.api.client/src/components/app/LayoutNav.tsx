"use client";
import { usePathname } from "next/navigation";
import { LayoutNavConst } from "@/const/LayoutNavConst";
import Link from "next/link";

export default function LayoutNav() {
  const pathname = usePathname();
  return (
    <ul className="menu menu-horizontal px-1">
      {LayoutNavConst?.map(({ name, key, href }) => {
        const path = "/" + pathname?.split("/")[1] || "";

        const isActive = path === href;
        return (
          <li key={key}>
            <Link
              href={href}
              aria-label={name}
              className={`${isActive ? "underline underline-offset-6" : "hover:underline hover:underline-offset-6"} hover:!bg-transparent transition-all text-base`}
            >
              {name}
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
