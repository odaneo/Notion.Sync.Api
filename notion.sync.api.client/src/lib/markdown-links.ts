import { posix } from "node:path";
import { articlePath } from "./blog-path.ts";

type ArticleReference = { title: string; slug: string; sourcePath: string };
type MarkdownNode = {
  type: string;
  value?: string;
  url?: string;
  children?: MarkdownNode[];
};

export function resolveArticleLink(
  target: string,
  sourcePath: string,
  articles: ArticleReference[],
) {
  const [file, ...fragmentParts] = target.split("#");
  const fragment = fragmentParts.join("#");
  if (!file) return fragment ? `#${encodeURIComponent(fragment)}` : null;
  let decoded: string;
  try {
    decoded = decodeURIComponent(file);
  } catch {
    return null;
  }
  const normalize = (value: string) =>
    value
      .replace(/\\/g, "/")
      .replace(/\.md$/i, "")
      .normalize("NFC")
      .toLowerCase();
  const name = normalize(decoded);
  const relative = normalize(
    posix.normalize(posix.join(posix.dirname(sourcePath), decoded)),
  );
  const exact = articles.filter((article) =>
    [name, relative].includes(normalize(article.sourcePath)),
  );
  const matches = exact.length
    ? exact
    : articles.filter((article) =>
        [
          normalize(article.title),
          normalize(article.slug),
          normalize(posix.basename(article.sourcePath)),
        ].includes(name),
      );
  if (matches.length !== 1) return null;
  return (
    articlePath(matches[0].slug) +
    (fragment ? `#${encodeURIComponent(fragment)}` : "")
  );
}

export function remarkArticleLinks(
  articles: ArticleReference[],
  sourcePath: string,
) {
  return () => (tree: MarkdownNode) => {
    function visit(node: MarkdownNode) {
      if (
        node.type === "link" &&
        node.url &&
        !/^[a-z][a-z\d+.-]*:/i.test(node.url) &&
        /\.md(?:#|$)/i.test(node.url)
      ) {
        const url = resolveArticleLink(node.url, sourcePath, articles);
        if (url) node.url = url;
        else {
          node.type = "emphasis";
          delete node.url;
        }
      }
      if (
        !node.children ||
        node.type === "link" ||
        node.type === "code" ||
        node.type === "inlineCode"
      )
        return;
      node.children = node.children.flatMap((child): MarkdownNode[] => {
        if (child.type !== "text" || !child.value) {
          visit(child);
          return [child];
        }
        const nodes: MarkdownNode[] = [];
        let end = 0;
        for (const match of child.value.matchAll(/\[\[([^\]\n]+)\]\]/g)) {
          const start = match.index!;
          if (start > end)
            nodes.push({ type: "text", value: child.value.slice(end, start) });
          const [target, ...alias] = match[1].split("|");
          const label =
            alias.join("|") ||
            posix.basename(target.replace(/\.md(?=#|$)/i, ""));
          const url = resolveArticleLink(target, sourcePath, articles);
          nodes.push(
            url
              ? {
                  type: "link",
                  url,
                  children: [{ type: "text", value: label }],
                }
              : { type: "text", value: label },
          );
          end = start + match[0].length;
        }
        if (!end) return [child];
        if (end < child.value.length)
          nodes.push({ type: "text", value: child.value.slice(end) });
        return nodes;
      });
    }
    visit(tree);
  };
}
