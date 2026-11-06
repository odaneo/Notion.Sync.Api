import { articlePath } from "@/lib/blog-path";
import Link from "next/link";
import { ChevronDown } from "lucide-react";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";

export default function MenuList({
  tags,
}: {
  tags: GetTagsWithArticlesResponseType[];
}) {
  return (
    <ul className="tag-groups">
      {tags.length === 0 && <li className="entry-date">暂无分类</li>}
      {tags.map((tag) => (
        <li key={tag.slug}>
          <details open>
            <summary>
              <span>{tag.title}</span>
              <span className="entry-date">{tag.articles.length} 篇</span>
              <ChevronDown size={16} aria-hidden="true" />
            </summary>
            <ul className="tag-articles">
              {tag.articles.map((article) => (
                <li key={article.id}>
                  <Link href={articlePath(article.slug)}>{article.title}</Link>
                  {article.subTags?.map(({ id, title }) => (
                    <span key={id} className="tag-pill">
                      {title}
                    </span>
                  ))}
                </li>
              ))}
            </ul>
          </details>
        </li>
      ))}
    </ul>
  );
}
