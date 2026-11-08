import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  agentRules: false,
  async redirects() {
    return [
      {
        source: "/blog/:tag/:slug",
        destination: "/blog/:slug",
        permanent: true,
      },
    ];
  },
  images: { unoptimized: true },
  transpilePackages: ["lucide-react"],
};

export default nextConfig;
