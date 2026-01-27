import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import MenuList from "@/components/tag/MenuList";

export default async function Tag() {
  const { data: tagsData } = await supabase
    .rpc("get_tags_with_articles_json")
    .overrideTypes<GetTagsWithArticlesResponseType[]>();

  const tags = Array.isArray(tagsData) ? tagsData : [];

  return (
    <main className="mt-5 w-full">
      <h3 className="text-2xl ml-4 mb-3 italic">所有标签</h3>
      {tags && <MenuList tags={tags} />}
    </main>
  );
}
