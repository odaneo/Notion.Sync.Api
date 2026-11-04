import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  agentRules: false,
  images: { unoptimized: true },
  transpilePackages: ["lucide-react"],
};

export default nextConfig;
