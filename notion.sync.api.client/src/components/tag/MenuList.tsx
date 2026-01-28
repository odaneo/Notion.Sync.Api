"use client";
import Link from "next/link";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";

export default function MenuList({
  tags,
}: {
  tags: GetTagsWithArticlesResponseType[];
}) {
  return (
    <ul className="menu block w-full py-0 px-10">
      {tags?.length === 0 && (
        <li>
          <a>暂无分类</a>
        </li>
      )}
      {tags?.map((tag) => (
        <li key={tag.slug}>
          <details open>
            <summary>
              <h3 className="truncate font-semibold text-xl">{tag.title}</h3>
              <span className="ml-auto text-slate-500">
                {tag.articles?.length ?? 0} 篇文章
              </span>
            </summary>
            {tag.articles?.length ? (
              <ul>
                {tag.articles.map((a) => {
                  const href = `/blog/${encodeURIComponent(tag.slug)}/${encodeURIComponent(
                    a.slug,
                  )}`;
                  return (
                    <li key={a.id}>
                      <div className="flex">
                        <Link
                          className={`line-clamp-2 break-words text-lg flex-1`}
                          href={href}
                        >
                          <h4>{a.title}</h4>
                        </Link>
                        {a.subTags?.map((t) => (
                          <span
                            key={t.id}
                            className="badge badge-primary rounded"
                          >
                            {t.title}
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
