import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  transpilePackages: ["react-notion-x", "notion-types", "notion-utils"],
};

export default nextConfig;
