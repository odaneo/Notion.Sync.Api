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
        <div className="mb-6 border-b border-slate-200">
          <div className="flex items-center gap-3 py-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-600">
              <svg
                viewBox="0 0 24 24"
                className="h-6 w-6"
                fill="none"
                aria-hidden
              >
                <path
                  d="M3 6.75A2.75 2.75 0 0 1 5.75 4H11v16H5.75A2.75 2.75 0 0 1 3 17.25V6.75z"
                  stroke="#fff"
                  strokeWidth="1.6"
                  strokeLinejoin="round"
                />
                <path
                  d="M21 6.75A2.75 2.75 0 0 0 18.25 4H13v16h5.25A2.75 2.75 0 0 0 21 17.25V6.75z"
                  stroke="#fff"
                  strokeWidth="1.6"
                  strokeLinejoin="round"
                />
                <path
                  d="M11 6h2M11 10h2M11 14h2"
                  stroke="#fff"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                />
              </svg>
            </div>
            <div>
              <div className="text-2xl font-bold leading-6 tracking-tight text-slate-800">
                技术博客
              </div>
              <div className="text-sm text-slate-500">
                分享前沿技术与深度思考
              </div>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-12">
          <aside className="md:col-span-4 lg:col-span-3">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <div className="mb-3 text-sm font-medium text-slate-600">
                分类
              </div>
              <nav className="space-y-1">
                {tags.length === 0 && (
                  <div className="text-sm text-slate-500">暂无分类</div>
                )}
                {tags.map((tag) => (
                  <div key={tag.Slug}>
                    <div className="flex items-center justify-between rounded-md px-3 py-2 text-sm text-slate-700">
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
                              className="block truncate rounded-md px-3 py-1.5 text-sm text-slate-600 transition-colors hover:bg-slate-100"
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
            <div className="rounded-lg border border-slate-200 p-4">
              {children}
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}
