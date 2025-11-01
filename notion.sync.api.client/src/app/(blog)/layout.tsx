import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import MenuList from "@/components/MenuList";

export const dynamic = "force-dynamic";

type LayoutProps = {
  children: React.ReactNode;
};

export default async function BlogLayout({ children }: LayoutProps) {
  const { data: tagsData } = await supabase
    .rpc("get_tags_with_articles_json")
    .overrideTypes<GetTagsWithArticlesResponseType[]>();

  const tags = Array.isArray(tagsData) ? tagsData : [];

  return (
    <main className="mx-auto max-w-7xl p-6">
      <div className="grid grid-cols-1 gap-6 md:grid-cols-12">
        <aside className="col-span-3">
          <ul className="menu bg-base-200 rounded-box w-full">
            {tags.length === 0 && (
              <li>
                <a>暂无分类</a>
              </li>
            )}
            {tags.map((tag) => (
              <li key={tag.Slug}>
                <details open>
                  <summary>
                    <h3 className="truncate font-semibold text-lg">
                      {tag.Title}
                    </h3>
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

                        return (
                          <MenuList
                            key={a.Id}
                            href={href}
                            title={a.Title}
                            slug={a.Slug}
                          />
                        );
                      })}
                    </ul>
                  ) : null}
                </details>
              </li>
            ))}
          </ul>
        </aside>

        <section className="col-span-9">
          <div className="card">
            <div className="card-body">{children}</div>
          </div>
        </section>
      </div>
    </main>
  );
}
