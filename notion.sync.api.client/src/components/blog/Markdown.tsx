import ReactMarkdown from "react-markdown";
import { Children, isValidElement, type ReactNode } from "react";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import rehypeSlug from "rehype-slug";
import { getAllArticles } from "@/lib/blog";
import { remarkArticleLinks } from "@/lib/markdown-links";
import Mermaid from "./Mermaid";
import "highlight.js/styles/github.css";

export default async function Markdown({
  content,
  sourcePath,
}: {
  content: string;
  sourcePath: string;
}) {
  const articles = await getAllArticles();
  return (
    <div className="markdown">
      <ReactMarkdown
        skipHtml
        remarkPlugins={[remarkGfm, remarkArticleLinks(articles, sourcePath)]}
        rehypePlugins={[
          rehypeSlug,
          [rehypeHighlight, { detect: false, ignoreMissing: true }],
        ]}
        components={{
          pre({ children }) {
            const [child] = Children.toArray(children);
            if (
              isValidElement<{ className?: string; children?: ReactNode }>(
                child,
              ) &&
              child.props.className?.split(/\s+/).includes("language-mermaid")
            ) {
              return (
                <Mermaid
                  source={String(child.props.children).replace(/\n$/, "")}
                />
              );
            }
            return <pre>{children}</pre>;
          },
          a({ children, href }) {
            return (
              <a href={href} rel="noopener noreferrer">
                {children}
              </a>
            );
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
