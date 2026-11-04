export function articlePath(slug: string) {
  return `/blog/${encodeURIComponent(slug)}`;
}

export function jsonLd(value: unknown) {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}
