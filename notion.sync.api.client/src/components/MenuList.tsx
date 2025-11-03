"use client";
import { usePathname } from "next/navigation";
import Link from "next/link";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import { useCallback } from "react";

export default function MenuList({
  tags,
}: {
  tags: GetTagsWithArticlesResponseType[];
}) {
  const pathname = usePathname();

  const handleMenuClick = useCallback(
    (e: React.MouseEvent<HTMLUListElement>) => {
      if (typeof window === "undefined") return;
      const isMobile = window.matchMedia("(max-width: 1023.98px)").matches;
      if (!isMobile) return;

      const target = e.target as HTMLElement;

      const anchor = target.closest("a[href]") as HTMLAnchorElement | null;
      if (!anchor) return;

      const input = document.getElementById(
        "my-drawer-3"
      ) as HTMLInputElement | null;
      if (input) input.checked = false;
    },
    []
  );

  return (
    <ul
      className="menu block bg-base-200 rounded-box w-60 overflow-y-auto max-h-[calc(100dvh-var(--header))]"
      onClick={handleMenuClick}
    >
      {tags?.length === 0 && (
        <li>
          <a>暂无分类</a>
        </li>
      )}
      {tags?.map((tag) => (
        <li key={tag.Slug}>
          <details open>
            <summary>
              <h3 className="truncate font-semibold text-lg">{tag.Title}</h3>
              <span className="ml-auto text-slate-500">
                {tag.Articles?.length ?? 0}
              </span>
            </summary>
            {tag.Articles?.length ? (
              <ul>
                {tag.Articles.map((a) => {
                  const href = `/${encodeURIComponent(tag.Slug)}/${encodeURIComponent(
                    a.Slug
                  )}`;
                  const isActive = pathname?.split("/")[2] === a.Slug;
                  return (
                    <li key={a.Id}>
                      <Link
                        className={`${isActive ? "menu-active" : ""} line-clamp-2 break-words`}
                        href={href}
                      >
                        {a.Title}
                      </Link>
                    </li>
                  );
                })}
              </ul>
            ) : null}
          </details>
        </li>
      ))}
    </ul>
  );
}
