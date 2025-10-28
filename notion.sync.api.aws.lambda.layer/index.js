import { getSecret } from "@aws-lambda-powertools/parameters/secrets";
import { Pool } from "pg";

const SECRET_ID = process.env.SECRET_ID;
const DB_HOST = process.env.DB_HOST;
const DB_PORT = process.env.DB_PORT ?? "5432";
const DB_NAME = process.env.DB_NAME;
const SECRET_TTL_MS = Number(process.env.SECRET_TTL_MS ?? 60000);

let pool;

function buildConnStr({ username, password }) {
	const user = encodeURIComponent(username);
	const pass = encodeURIComponent(password);
	return `postgresql://${user}:${pass}@${DB_HOST}:${DB_PORT}/${DB_NAME}`;
}

async function buildPool({ forceSecret = false } = {}) {
	const opts = forceSecret
		? { forceFetch: true }
		: { cacheExpiryInMillis: SECRET_TTL_MS };
	const { username, password } = JSON.parse(await getSecret(SECRET_ID, opts));
	const connectionString = buildConnStr({ username, password });
	return new Pool({
		connectionString,
		ssl: { rejectUnauthorized: false },
	});
}

async function getPool({ rebuild = false } = {}) {
	if (rebuild && pool) {
		try {
			await pool.end();
		} catch {}
		pool = undefined;
	}
	if (!pool) pool = await buildPool();
	return pool;
}

async function exec(sql, params = []) {
	const p = await getPool();
	try {
		return await p.query(sql, params);
	} catch (e) {
		const authError =
			e?.code === "28P01" ||
			/password|auth|expired|invalid/i.test(e?.message ?? "");
		if (authError) {
			pool = await buildPool({ forceSecret: true });
			return await pool.query(sql, params);
		}
		throw e;
	}
}

export { exec };
