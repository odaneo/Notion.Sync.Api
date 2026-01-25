import { ArticlesType } from "@/type/api.type";
import Link from "next/link";
import UpdatedAtJST from "./blog/UpdatedAtJST";

export default function ArticleList({
  Title,
  Slug,
  LastEditedTime,
  Tags,
  SubTags,
}: Omit<ArticlesType, "Id">) {
  return (
    <>
      <Link
        href={`/${Tags[0].Slug}/${Slug}`}
        className="text-lg link link-hover hover:underline-offset-3"
      >
        {Title}
      </Link>
      <div className="mt-2 mb-4 flex flex-wrap gap-3">
        {Tags?.map((t) => (
          <span key={t.Id} className="badge badge-soft badge-info rounded">
            {t.Title}
          </span>
        ))}
        {SubTags?.map((t) => (
          <span key={t.Id} className="badge badge-primary rounded">
            {t.Title}
          </span>
        ))}
        {LastEditedTime && <UpdatedAtJST date={LastEditedTime} />}
      </div>
    </>
  );
}
