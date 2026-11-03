-- Run with the migration connection and Search Path=blog_test.
-- This verifies constraints and privileges, then rolls back all sample rows.
BEGIN;
DO $verify$
DECLARE
    first_article text := gen_random_uuid()::text;
    second_article text := gen_random_uuid()::text;
    first_tag text := gen_random_uuid()::text;
    second_tag text := gen_random_uuid()::text;
    shared_subtag text := gen_random_uuid()::text;
    other_subtag text := gen_random_uuid()::text;
    run_id text := gen_random_uuid()::text;
    sample_count integer;
BEGIN
    IF current_schema() IS DISTINCT FROM 'blog_test' THEN
        RAISE EXCEPTION 'Verification must target blog_test';
    END IF;
    IF EXISTS (SELECT FROM information_schema.columns
        WHERE table_schema = 'blog_test' AND table_name = 'SubTags' AND column_name = 'TagId') THEN
        RAISE EXCEPTION 'SubTags still has a parent TagId';
    END IF;
    IF has_schema_privilege('blog_test_writer', 'blog_test', 'CREATE') OR EXISTS (
        SELECT FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname IN ('public', 'blog') AND c.relkind IN ('r', 'p', 'v')
        AND has_table_privilege('blog_test_writer', c.oid, 'INSERT,UPDATE,DELETE,TRUNCATE')) THEN
        RAISE EXCEPTION 'Test writer has DDL or production write privileges';
    END IF;

    -- PostgreSQL 17 does not automatically let a role creator SET ROLE.
    -- Enable it only inside this verification transaction; ROLLBACK restores membership.
    EXECUTE format('GRANT blog_test_writer TO %I WITH INHERIT FALSE, SET TRUE', session_user);
    SET LOCAL ROLE blog_test_writer;
    INSERT INTO "Tags" ("Id", "Title", "Slug", "LastEditedTime") VALUES
        (first_tag, 'Verify A', 'verify-' || first_tag, now()),
        (second_tag, 'Verify B', 'verify-' || second_tag, now());
    INSERT INTO "SubTags" ("Id", "Title", "Slug", "LastEditedTime") VALUES
        (shared_subtag, 'Shared', 'verify-' || shared_subtag, now()),
        (other_subtag, 'Other', 'verify-' || other_subtag, now());
    INSERT INTO "Articles" ("Id", "Title", "Slug", "Content", "SourcePath", "ContentHash", "LastEditedTime") VALUES
        (first_article, 'Verify one', 'verify-' || first_article, '# Markdown', 'verify/' || first_article, repeat('f', 64), now()),
        (second_article, 'Verify two', 'verify-' || second_article, '# Markdown', 'verify/' || second_article, repeat('e', 64), now());
    INSERT INTO "ArticleTags" ("ArticleId", "TagId") VALUES
        (first_article, first_tag), (first_article, second_tag), (second_article, second_tag);
    INSERT INTO "ArticleSubTags" ("ArticleId", "SubTagId") VALUES
        (first_article, shared_subtag), (first_article, other_subtag), (second_article, shared_subtag);
    SELECT count(*) INTO sample_count FROM "ArticleSubTags" WHERE "SubTagId" = shared_subtag;
    IF sample_count <> 2 THEN RAISE EXCEPTION 'Shared subtag relation failed'; END IF;
    SELECT count(*) INTO sample_count FROM "ArticleTags" WHERE "ArticleId" = first_article;
    IF sample_count <> 2 THEN RAISE EXCEPTION 'Multiple tags per article failed'; END IF;

    BEGIN
        INSERT INTO "ArticleTags" ("ArticleId", "TagId") VALUES (first_article, first_tag);
        RAISE EXCEPTION 'Duplicate association accepted';
    EXCEPTION WHEN unique_violation THEN NULL; END;
    BEGIN
        INSERT INTO "ArticleTags" ("ArticleId", "TagId") VALUES (gen_random_uuid()::text, first_tag);
        RAISE EXCEPTION 'Orphan association accepted';
    EXCEPTION WHEN foreign_key_violation THEN NULL; END;
    BEGIN
        UPDATE "Articles" SET "Slug" = 'verify-' || first_article WHERE "Id" = second_article;
        RAISE EXCEPTION 'Duplicate slug accepted';
    EXCEPTION WHEN unique_violation THEN NULL; END;
    BEGIN
        CREATE TABLE blog_test."VerifyForbidden" (id integer);
        RAISE EXCEPTION 'Writer was allowed to create a table';
    EXCEPTION WHEN insufficient_privilege THEN NULL; END;

    INSERT INTO "SyncRuns" ("Id", "Repository", "CommitSha", "StartedAt")
        VALUES (run_id, 'verify/' || run_id, repeat('a', 40), now());
    BEGIN
        UPDATE "SyncRuns" SET "CacheInvalidatedAt" = now() WHERE "Id" = run_id;
        RAISE EXCEPTION 'Cache invalidation was accepted before database application';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        INSERT INTO "SyncRuns" ("Id", "Repository", "CommitSha", "StartedAt")
            VALUES (gen_random_uuid()::text, 'verify/' || run_id, repeat('a', 40), now());
        RAISE EXCEPTION 'Duplicate repository/commit accepted';
    EXCEPTION WHEN unique_violation THEN NULL; END;

    SET LOCAL ROLE anon;
    SELECT count(*) INTO sample_count FROM "Articles" WHERE "Id" IN (first_article, second_article);
    IF sample_count <> 2 THEN RAISE EXCEPTION 'Anonymous read failed'; END IF;
    BEGIN
        DELETE FROM "Articles" WHERE "Id" = first_article;
        RAISE EXCEPTION 'Anonymous write accepted';
    EXCEPTION WHEN insufficient_privilege THEN NULL; END;
    BEGIN
        PERFORM 1 FROM "SyncRuns";
        RAISE EXCEPTION 'Sync run details were exposed';
    EXCEPTION WHEN insufficient_privilege THEN NULL; END;
    BEGIN
        PERFORM 1 FROM "__EFMigrationsHistory";
        RAISE EXCEPTION 'Migration history was exposed';
    EXCEPTION WHEN insufficient_privilege THEN NULL; END;

    SET LOCAL ROLE blog_test_writer;
    DELETE FROM "Articles" WHERE "Id" = first_article;
    IF EXISTS (SELECT FROM "ArticleTags" WHERE "ArticleId" = first_article)
        OR EXISTS (SELECT FROM "ArticleSubTags" WHERE "ArticleId" = first_article) THEN
        RAISE EXCEPTION 'Article deletion left orphan associations';
    END IF;
    IF NOT EXISTS (SELECT FROM "SubTags" WHERE "Id" = shared_subtag) THEN
        RAISE EXCEPTION 'Article deletion removed shared taxonomy';
    END IF;
    RESET ROLE;
END
$verify$;
ROLLBACK;
