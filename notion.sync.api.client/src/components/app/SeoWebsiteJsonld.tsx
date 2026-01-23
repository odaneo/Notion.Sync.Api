"use client";
import Script from "next/script";

export default function SeoWebsiteJsonld() {
  return (
    <Script
      id="jsonld-website"
      type="application/ld+json"
      dangerouslySetInnerHTML={{
        __html: JSON.stringify({
          "@context": "https://schema.org",
          "@type": "WebSite",
          name: "街街的脏书包",
          url: `${process.env.HOME_URL}`,
          potentialAction: {
            "@type": "SearchAction",
            target: `${process.env.HOME_URL}/search?q={search_term_string}`,
            "query-input": "required name=search_term_string",
          },
        }),
      }}
    />
  );
}
