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
    <main className="mx-auto max-w-7xl">
      <div className="drawer lg:drawer-open">
        <input id="my-drawer-3" type="checkbox" className="drawer-toggle" />
        <div className="drawer-content">
          <label
            htmlFor="my-drawer-3"
            className="btn drawer-button w-full h-10 z-5 sticky top-[var(--header)] justify-start bg-transparent border-transparent lg:hidden"
          >
            <div className="flex flex-col gap-[3px]">
              <span className="block w-4 h-[2px] bg-gray-600"></span>
              <span className="block w-4 h-[2px] bg-gray-600"></span>
              <span className="block w-4 h-[2px] bg-gray-600"></span>
            </div>
          </label>

          <div className="card">
            <div className="card-body">{children}</div>
          </div>
        </div>
        <div className="drawer-side h-[calc(100dvh-var(--header))] top-[var(--header)] bottom-0">
          <label
            htmlFor="my-drawer-3"
            aria-label="close sidebar"
            className="drawer-overlay"
          ></label>
          {tags && <MenuList tags={tags} />}
        </div>
      </div>
    </main>
  );
}
