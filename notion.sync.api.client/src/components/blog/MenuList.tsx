"use client";
import Link from "next/link";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import { useCallback } from "react";

export default function MenuList({
  tags,
}: {
  tags: GetTagsWithArticlesResponseType[];
}) {
  const handleMenuClick = useCallback(
    (e: React.MouseEvent<HTMLUListElement>) => {
      if (typeof window === "undefined") return;
      const isMobile = window.matchMedia("(max-width: 639.98px)").matches;
      if (!isMobile) return;

      const target = e.target as HTMLElement;

      const anchor = target.closest("a[href]") as HTMLAnchorElement | null;
      if (!anchor) return;

      const input = document.getElementById(
        "my-drawer-3",
      ) as HTMLInputElement | null;
      if (input) input.checked = false;
    },
    [],
  );

  return (
    <ul className="menu block w-full" onClick={handleMenuClick}>
      {tags?.length === 0 && (
        <li>
          <a>暂无分类</a>
        </li>
      )}
      {tags?.map((tag) => (
        <li key={tag.Slug}>
          <details open>
            <summary>
              <h3 className="truncate font-semibold text-2xl">{tag.Title}</h3>
              <span className="ml-auto text-slate-500">
                {tag.Articles?.length ?? 0} 篇文章
              </span>
            </summary>
            {tag.Articles?.length ? (
              <ul>
                {tag.Articles.map((a) => {
                  const href = `/${encodeURIComponent(tag.Slug)}/${encodeURIComponent(
                    a.Slug,
                  )}`;
                  return (
                    <li key={a.Id}>
                      <div>
                        <Link
                          className={`line-clamp-2 break-words text-lg`}
                          href={href}
                        >
                          {a.Title}
                        </Link>
                        {a.SubTags?.map((t) => (
                          <span
                            key={t.Id}
                            className="badge badge-primary rounded"
                          >
                            {t.Title}
                          </span>
                        ))}
                      </div>
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
