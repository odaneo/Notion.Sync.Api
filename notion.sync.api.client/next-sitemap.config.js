/** @type {import('next-sitemap').IConfig} */
module.exports = {
  siteUrl: `${process.env.HOME_URL}`,
  generateRobotsTxt: true,
  exclude: ["/server-sitemap.xml"],
  robotsTxtOptions: {
    additionalSitemaps: [`${process.env.HOME_URL}/server-sitemap.xml`],
  },
};
