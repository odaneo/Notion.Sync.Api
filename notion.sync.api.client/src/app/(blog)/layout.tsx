import Link from "next/link";
import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";

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
    <main className="min-h-screen">
      <div className="mx-auto max-w-7xl p-4 sm:p-6 lg:p-8">
        <div className="mb-4 flex items-center justify-between">
          <h1 className="text-xl font-semibold tracking-tight text-slate-800 dark:text-slate-100">
            博客
          </h1>
        </div>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-12">
          <aside className="md:col-span-4 lg:col-span-3">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/40">
              <div className="mb-3 text-sm font-medium text-slate-600 dark:text-slate-300">
                分类
              </div>
              <nav className="space-y-1">
                {tags.length === 0 && (
                  <div className="text-sm text-slate-500">暂无分类</div>
                )}
                {tags.map((tag) => (
                  <div key={tag.Slug}>
                    <div
                      className={`flex items-center justify-between rounded-md px-3 py-2 text-sm ${"text-slate-700 dark:text-slate-200"}`}
                    >
                      <span className="truncate font-medium">{tag.Title}</span>
                      <span className="text-xs text-slate-500">
                        {tag.Articles?.length ?? 0}
                      </span>
                    </div>

                    {tag.Articles?.length ? (
                      <div className="mt-1 space-y-1 pl-2">
                        {tag.Articles.map((a) => {
                          const href = `/${encodeURIComponent(tag.Slug)}/${encodeURIComponent(
                            a.Slug
                          )}/${encodeURIComponent(a.Id)}`;

                          return (
                            <Link
                              key={a.Id}
                              href={href}
                              className={`block truncate rounded-md px-3 py-1.5 text-sm transition-colors ${"text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800/60"}`}
                            >
                              {a.Title}
                            </Link>
                          );
                        })}
                      </div>
                    ) : null}
                  </div>
                ))}
              </nav>
            </div>
          </aside>

          <section className="md:col-span-8 lg:col-span-9">
            <div className="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
              {children}
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}
