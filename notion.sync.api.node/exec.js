import {
	SecretsManagerClient,
	GetSecretValueCommand,
} from "@aws-sdk/client-secrets-manager";
import { Pool } from "pg";

const IS_LOCAL = process.env.ENV === "LOCAL";
const SECRET_ID = process.env.SECRET_ID;
const DB_HOST = process.env.DB_HOST;
const DB_PORT = process.env.DB_PORT ?? "5432";
const DB_NAME = process.env.DB_NAME;

let pool;

const sm = new SecretsManagerClient({});

function buildConnStr({ username, password }) {
	const user = encodeURIComponent(username);
	const pass = encodeURIComponent(password);
	return `postgresql://${user}:${pass}@${DB_HOST}:${DB_PORT}/${DB_NAME}`;
}

async function buildPool() {
	if (IS_LOCAL) {
		const connectionString = buildConnStr({
			username: "postgres",
			password: "postgres",
		});
		return new Pool({
			connectionString,
			ssl: false,
		});
	} else {
		const res = await sm.send(
			new GetSecretValueCommand({ SecretId: SECRET_ID }),
		);
		const { username, password } = JSON.parse(res.SecretString);
		const connectionString = buildConnStr({ username, password });

		return new Pool({
			connectionString,
			ssl: { rejectUnauthorized: false },
		});
	}
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
			pool = await buildPool();
			return await pool.query(sql, params);
		}
		throw e;
	}
}

export { exec };
