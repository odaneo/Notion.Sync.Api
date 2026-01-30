import { revalidatePath } from "next/cache";
import { NextRequest, NextResponse } from "next/server";
import { supabase } from "@/utils/supabase/server";
import { GetTagsWithArticlesResponseType } from "@/type/api.type";
import { waitUntil } from "@vercel/functions";

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

export async function GET(req: NextRequest) {
  const authHeader = req.headers.get("x-revalidation-secret");

  if (!authHeader || authHeader !== process.env.LAMBDA_KEY) {
    return new Response("Unauthorized", { status: 401 });
  }

  try {
    revalidatePath("/", "layout");

    const { data: tagsData } = await supabase
      .rpc("get_tags_with_articles_json")
      .overrideTypes<GetTagsWithArticlesResponseType[]>();

    const tags = Array.isArray(tagsData) ? tagsData : [];

    const urlsToWarm = [
      `${process.env.HOME_URL}`,
      `${process.env.HOME_URL}/blog`,
      `${process.env.HOME_URL}/tag`,
      `${process.env.HOME_URL}/contact`,
    ];

    tags.forEach((tag) => {
      urlsToWarm.push(
        `${process.env.HOME_URL}/tag/${encodeURIComponent(tag.slug)}`,
      );
      tag.articles?.forEach((article) => {
        const url = `${process.env.HOME_URL}/blog/${encodeURIComponent(
          tag.slug,
        )}/${encodeURIComponent(article.slug)}`;

        urlsToWarm.push(`${url}`);
      });
    });

    const batchWarmup = async () => {
      await delay(1000);

      for (let i = 0; i < urlsToWarm.length; i += 5) {
        const batch = urlsToWarm.slice(i, i + 5);
        await Promise.all(
          batch.map((url) =>
            fetch(url as string, {
              headers: {
                "x-warmup": "true",
                "User-Agent": "warmup-bot",
              },
              cache: "no-store",
            }).catch((e) => console.error(`Failed: ${url}`, e)),
          ),
        );
        await delay(100);
      }
    };

    waitUntil(batchWarmup());

    return NextResponse.json({
      status: "success",
      count: urlsToWarm.length,
    });
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    return NextResponse.json({ error: message }, { status: 500 });
  }
}
