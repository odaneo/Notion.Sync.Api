import { ArticlesType } from "@/type/api.type";
import Link from "next/link";
import UpdatedAtJST from "./UpdatedAtJST";

export default function ArticleList({
  title,
  slug,
  lastEditedTime,
  tags,
  subTags,
}: Omit<ArticlesType, "id">) {
  return (
    <>
      <Link
        href={`/blog/${tags[0].slug}/${slug}`}
        className="text-lg link link-hover hover:underline-offset-3"
      >
        <h3>{title}</h3>
      </Link>
      <div className="mt-2 mb-4 flex flex-wrap gap-3">
        {tags?.map((t) => (
          <span key={t.id} className="badge badge-soft badge-info rounded">
            <Link key={t.id} href={`/tag/${t.slug}`}>
              {t.title}
            </Link>
          </span>
        ))}
        {subTags?.map((t) => (
          <span key={t.id} className="badge badge-primary rounded">
            {t.title}
          </span>
        ))}
        {lastEditedTime && <UpdatedAtJST date={lastEditedTime} />}
      </div>
    </>
  );
}
