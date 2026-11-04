"use client";

import { useEffect, useId, useState } from "react";

export default function Mermaid({ source }: { source: string }) {
  const id = useId().replace(/[^a-zA-Z0-9]/g, "");
  const [svg, setSvg] = useState("");
  useEffect(() => {
    let cancelled = false;
    async function render() {
      try {
        const { default: mermaid } = await import("mermaid");
        mermaid.initialize({
          startOnLoad: false,
          securityLevel: "strict",
          theme: "neutral",
          suppressErrorRendering: true,
        });
        const result = await mermaid.render(`mermaid-${id}`, source);
        if (!cancelled) setSvg(result.svg);
      } catch {
        // Keep the readable source when a diagram is invalid or the renderer cannot load.
      }
    }
    void render();
    return () => {
      cancelled = true;
    };
  }, [id, source]);
  return svg ? (
    <div
      className="mermaid-diagram"
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  ) : (
    <pre>
      <code>{source}</code>
    </pre>
  );
}
