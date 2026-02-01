import express from "express";
import { NotionAPI } from "notion-client";
import { exec } from "./exec.js";

const notion = new NotionAPI({
	activeUser: process.env.NOTION_ACTIVE_USER,
	authToken: process.env.NOTION_TOKEN_V2,
});

const getSql = `
SELECT
  "Id",
  "Content"
FROM public."Articles"
`;
const updateSql = `
UPDATE public."Articles"
SET "Content" = $1
WHERE "Id" = $2
`;

const app = express();
const PORT = 3000;

app.get("/update_notion_articles", async (_, res) => {
	if (!process.env.NOTION_TOKEN_V2) {
		return res.status(500).send("NOTION_TOKEN_V2 missing");
	}
	try {
		const { rows } = await exec(getSql);

		if (!rows || rows.length === 0) {
			return res.status(404).send("No articles found");
		}

		let success = 0;
		let failed = 0;

		for (const row of rows) {
			const rawId = row.Id;

			try {
				const recordMap = await notion.getPage(rawId);

				if (recordMap) {
					await exec(updateSql, [JSON.stringify(recordMap), rawId]);
					success++;
					console.log(`Updated article ${rawId}`);
				}
			} catch (err) {
				failed++;
				console.error(`Failed to update ${rawId}:`, err.message);
			}
		}

		const responseCatch = await fetch(`${process.env.REVALIDATE_URL}`, {
			method: "GET",
			headers: {
				"x-revalidation-secret": `${process.env.LAMBDA_KEY}`,
				"Content-Type": "application/json",
				"User-Agent":
					"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
			},
		});
		const dataCatch = await responseCatch.json();

		res.status(200).json({ success, failed, count: dataCatch.count });
	} catch (globalErr) {
		res.status(500).json({
			name: globalErr.name,
			message: globalErr.message,
			stack: globalErr.stack,
		});
	}
});

app.listen(PORT, () => {
	console.log(`node --env-file=.env index.js http://localhost:${PORT}`);
});
