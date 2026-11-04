const fs = require('node:fs');
const path = require('node:path');
const { parseEnv } = require('node:util');

async function main() {
  const args = process.argv.slice(2);
  const port = Number(args[0] ?? 3000);
  if (args.length > 1 || !Number.isInteger(port) || port < 1 || port > 65535)
    throw new Error('用法：node scripts/revalidate-local.cjs [端口]，例如 node scripts/revalidate-local.cjs 8787。');
  const origin = `http://localhost:${port}`;
  const envFile = path.join(__dirname, '../notion.sync.api.client/.env.local');
  const { REVALIDATION_TOKEN: token } = parseEnv(fs.readFileSync(envFile, 'utf8'));
  if (!token || token.length < 32 || /[\r\n]/.test(token))
    throw new Error('请在 notion.sync.api.client/.env.local 配置有效的 REVALIDATION_TOKEN（至少 32 字符）。');

  let response;
  try {
    response = await fetch(`${origin}/api/revalidate`, {
      method: 'POST',
      headers: { authorization: 'Bearer ' + token },
      redirect: 'error',
      signal: AbortSignal.timeout(30000),
    });
  } catch {
    throw new Error(`无法请求本地刷新接口，请确认 pnpm dev 或 pnpm cf:preview 已启动在 ${origin}。`);
  }
  const result = await response.json().catch(() => null);
  if (!response.ok || result?.revalidated !== true)
    throw new Error(`本地缓存刷新失败（HTTP ${response.status}），请检查前端日志及 REVALIDATION_TOKEN 配置。`);
  console.log(JSON.stringify({ revalidated: true }));
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
