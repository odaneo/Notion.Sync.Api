"use client";

import type { ExtendedRecordMap } from "notion-types";
import { NotionRenderer } from "react-notion-x";
import dynamic from "next/dynamic";
import "react-notion-x/src/styles.css";
import "prismjs/themes/prism.css";
import "katex/dist/katex.min.css";

const Code = dynamic(
  () => import("react-notion-x/build/third-party/code").then((m) => m.Code),
  { ssr: true }
);
const Collection = dynamic(
  () =>
    import("react-notion-x/build/third-party/collection").then(
      (m) => m.Collection
    ),
  { ssr: true }
);
const Equation = dynamic(
  () =>
    import("react-notion-x/build/third-party/equation").then((m) => m.Equation),
  { ssr: true }
);

export default function NotionSSR({
  recordMap,
}: {
  recordMap: ExtendedRecordMap;
}) {
  return (
    <NotionRenderer
      recordMap={recordMap}
      fullPage={false}
      darkMode={false}
      disableHeader
      components={{ Code, Collection, Equation }}
    />
  );
}
