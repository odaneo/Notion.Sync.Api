const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const http = require('node:http');
const { once } = require('node:events');
const { execFileSync, spawn } = require('node:child_process');
const assert = require('node:assert/strict');
const { setTimeout: delay } = require('node:timers/promises');

// Local development only: no tokens on the command line or in extra env files.
async function main() {
  const args = process.argv.slice(2);
  if (args.length && (args.length !== 2 || args[0] !== '--vault' || !args[1] || args[1].startsWith('--')))
    throw new Error('Usage: node scripts/sync-local.cjs [--vault <directory>]');
  const vault = path.resolve(args[1] ?? path.join(os.homedir(), 'Documents', 'Obsidian'));
  const root = path.resolve(__dirname, '..');
  const project = path.join(root, 'Notion.Sync.Api');
  const projectFile = path.join(project, 'Notion.Sync.Api.csproj');
  const id = /<UserSecretsId>([^<]+)<\/UserSecretsId>/.exec(fs.readFileSync(projectFile, 'utf8'))?.[1];
  if (!id) throw new Error('Project UserSecretsId is missing');
  const secretFile = process.platform === 'win32'
    ? path.join(process.env.APPDATA, 'Microsoft', 'UserSecrets', id, 'secrets.json')
    : path.join(os.homedir(), '.microsoft', 'usersecrets', id, 'secrets.json');
  const secrets = JSON.parse(fs.readFileSync(secretFile, 'utf8').replace(/^\uFEFF/, ''));
  const token = secrets.Sync?.Token ?? secrets['Sync:Token'];
  const refreshToken = secrets.Revalidation?.Token ?? secrets['Revalidation:Token'];
  if (typeof token !== 'string' || token.length < 32 || typeof refreshToken !== 'string' || refreshToken.length < 32)
    throw new Error('Configure Sync:Token and Revalidation:Token in the existing User Secrets file');
  const { buildSnapshot } = require(path.join(vault, '.github', 'scripts', 'build-snapshot.cjs'));

  const working = path.join(root, '.dotnet-home');
  fs.mkdirSync(working, { recursive: true });
  execFileSync('dotnet', ['build', projectFile, '--no-restore', '--nologo', '-v:q'], {
    cwd: root, stdio: 'inherit', windowsHide: true,
    env: { ...process.env, DOTNET_CLI_HOME: working }
  });

  let api;
  let refreshRequests = 0;
  const mock = http.createServer((request, response) => {
    request.resume();
    if (request.method !== 'POST' || request.url !== '/api/revalidate') { response.writeHead(404).end(); return; }
    if (request.headers.authorization !== 'Bearer ' + refreshToken) { response.writeHead(401).end(); return; }
    refreshRequests++;
    response.writeHead(refreshRequests === 1 ? 503 : 200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ revalidated: refreshRequests !== 1 }));
  });
  const logPath = path.join(working, 'sync-local.log');
  const log = fs.openSync(logPath, 'w');
  const stop = () => api?.kill();
  process.once('SIGINT', stop);
  process.once('SIGTERM', stop);
  try {
    mock.listen(0, '127.0.0.1'); await once(mock, 'listening');
    // Reserve a free loopback port; startup fails safely if another process wins the port.
    const reserve = http.createServer();
    reserve.listen(0, '127.0.0.1'); await once(reserve, 'listening');
    const port = reserve.address().port;
    await new Promise(resolve => reserve.close(resolve));
    const origin = 'http://127.0.0.1:' + port;
    const command = [path.join(project, 'bin', 'Debug', 'net8.0', 'Notion.Sync.Api.dll'),
      '--environment', 'Development', '--urls', origin, '--Database:Schema', 'blog_test',
      '--Revalidation:Url', 'http://127.0.0.1:' + mock.address().port + '/api/revalidate'];
    api = spawn('dotnet', command, { cwd: project, windowsHide: true, stdio: ['ignore', log, log],
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development' } });
    let spawnError;
    api.on('error', error => { spawnError = error; });
    let ready = false;
    for (let attempt = 0; attempt < 120; attempt++) {
      if (spawnError || api.exitCode !== null) throw new Error('Local API startup failed; see ' + logPath);
      try { ready = (await fetch(origin + '/api/health', { redirect: 'error', signal: AbortSignal.timeout(1000) })).ok; } catch {}
      if (ready) break;
      await delay(250);
    }
    if (!ready) throw new Error('Local API startup timed out; see ' + logPath);
    const endpoint = origin + '/api/sync/obsidian';
    const headers = { authorization: 'Bearer ' + token, 'content-type': 'application/json' };
    const stateResponse = await fetch(endpoint + '/state', { headers, redirect: 'error', signal: AbortSignal.timeout(30000) });
    if (!stateResponse.ok) throw new Error('Could not read local sync state: HTTP ' + stateResponse.status);
    const state = await stateResponse.json();
    assert.equal(state.environment, 'Development'); assert.equal(state.schema, 'blog_test');
    assert(Number.isSafeInteger(state.lastAppliedRunNumber) && state.lastAppliedRunNumber >= 0);
    const snapshot = buildSnapshot(vault, state.repository, state.lastAppliedRunNumber + 1, state.branch, { allowDirty: true });
    console.log(JSON.stringify({ vault, schema: state.schema, articles: snapshot.articles.length, tags: snapshot.tags.length,
      subTags: snapshot.subTags.length }));
    const payload = JSON.stringify(snapshot);
    const send = async () => {
      const response = await fetch(endpoint, { method: 'POST', headers, body: payload,
        redirect: 'error', signal: AbortSignal.timeout(120000) });
      if (response.status !== 200 && response.status !== 503) throw new Error('Local sync returned HTTP ' + response.status);
      const result = await response.json();
      if (response.status === 503 && result.status !== 'cache_pending') throw new Error('Database sync failed; see ' + logPath);
      if (!['applied', 'unchanged', 'cache_pending'].includes(result.status)) throw new Error('Snapshot was not applied; rerun local sync');
      console.log(JSON.stringify(result));
      return result;
    };
    let result = await send();
    let refreshRetryVerified = false;
    if (result.status === 'cache_pending') {
      result = await send();
      assert.equal(result.status, 'unchanged'); assert.equal(result.updated, 0); assert.equal(result.cacheRefreshed, true);
      refreshRetryVerified = true;
    }
    const previousRequests = refreshRequests;
    const repeated = await send();
    assert.equal(repeated.status, 'unchanged'); assert.equal(repeated.added + repeated.updated + repeated.deleted, 0);
    assert.equal(repeated.cacheRefreshed, false); assert.equal(refreshRequests, previousRequests);
    console.log(JSON.stringify({ verified: true, refreshRetryVerified, mockRefreshRequests: refreshRequests }));
  } finally {
    process.removeListener('SIGINT', stop); process.removeListener('SIGTERM', stop);
    if (api && api.exitCode === null && api.pid) {
      const exited = once(api, 'exit'); api.kill(); await exited;
    }
    if (mock.listening) { mock.closeAllConnections(); await new Promise(resolve => mock.close(resolve)); }
    fs.closeSync(log);
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
