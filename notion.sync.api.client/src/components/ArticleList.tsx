import { articlePath } from "@/lib/blog-path";
import { ArticlesType } from "@/type/api.type";
import Link from "next/link";
import { ArrowUpRight } from "lucide-react";
import UpdatedAtJST from "./UpdatedAtJST";

export default function ArticleList({
  title,
  slug,
  lastEditedTime,
  tags,
  subTags,
}: Omit<ArticlesType, "id">) {
  return (
    <div className="article-entry">
      <Link href={articlePath(slug)} className="entry-title">
        <span>{title}</span>
        <ArrowUpRight size={17} aria-hidden="true" />
      </Link>
      <div className="entry-meta">
        {lastEditedTime && <UpdatedAtJST date={lastEditedTime} />}
        {tags?.map(({ id, slug, title }) => (
          <Link
            key={id}
            className="tag-pill"
            href={`/tag/${encodeURIComponent(slug)}`}
          >
            {title}
          </Link>
        ))}
        {subTags?.map(({ id, title }) => (
          <span key={id} className="tag-pill">
            {title}
          </span>
        ))}
      </div>
    </div>
  );
}
