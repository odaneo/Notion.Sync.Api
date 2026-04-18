/** @type {import('next-sitemap').IConfig} */
module.exports = {
  siteUrl: `${process.env.HOME_URL}`,
  generateRobotsTxt: false,
  exclude: ["/robots.txt", "/server-sitemap.xml"],
  robotsTxtOptions: {
    additionalSitemaps: [`${process.env.HOME_URL}/server-sitemap.xml`],
  },
};
