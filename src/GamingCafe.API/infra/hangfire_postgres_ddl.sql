-- Hangfire PostgreSQL DDL
--
-- Apply this script with a database user that has privileges to create schemas/tables/indexes.
-- Default schema: public. If you want a custom schema, open the file and replace "public" with your schema name
-- or create the schema beforehand and run the script.
--
-- Example (psql):
-- psql -h <host> -U <admin> -d <db> -f hangfire_postgres_ddl.sql

SET search_path TO public;

-- Optional: create schema if it doesn't exist (uncomment if you want to create a dedicated schema)
-- CREATE SCHEMA IF NOT EXISTS hangfire;
-- SET search_path TO hangfire;

-- Table: job
CREATE TABLE IF NOT EXISTS public."Job" (
    "Id" BIGSERIAL PRIMARY KEY,
    "StateId" BIGINT NULL,
    "InvocationData" TEXT NOT NULL,
    "Arguments" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExpireAt" TIMESTAMP WITHOUT TIME ZONE NULL
);
CREATE INDEX IF NOT EXISTS "IX_Job_ExpireAt" ON public."Job" ("ExpireAt");
CREATE INDEX IF NOT EXISTS "IX_Job_StateId" ON public."Job" ("StateId");

-- Table: state
CREATE TABLE IF NOT EXISTS public."State" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Reason" TEXT NULL,
    "Data" TEXT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "JobId" BIGINT NOT NULL REFERENCES public."Job" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_State_JobId" ON public."State" ("JobId");

-- Table: job_parameter
CREATE TABLE IF NOT EXISTS public."JobParameter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "JobId" BIGINT NOT NULL REFERENCES public."Job" ("Id") ON DELETE CASCADE,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_JobParameter_JobId" ON public."JobParameter" ("JobId");

-- Table: job_queue
CREATE TABLE IF NOT EXISTS public."JobQueue" (
    "Id" BIGSERIAL PRIMARY KEY,
    "JobId" BIGINT NOT NULL REFERENCES public."Job" ("Id") ON DELETE CASCADE,
    "Queue" TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_JobQueue_Queue" ON public."JobQueue" ("Queue");

-- Table: server
CREATE TABLE IF NOT EXISTS public."Server" (
    "Id" TEXT PRIMARY KEY,
    "Heartbeat" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Queues" TEXT NOT NULL,
    "LastSeen" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

-- Table: list
CREATE TABLE IF NOT EXISTS public."List" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_List_Key" ON public."List" ("Key");

-- Table: set
CREATE TABLE IF NOT EXISTS public."Set" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Set_Key" ON public."Set" ("Key");

-- Table: hash
CREATE TABLE IF NOT EXISTS public."Hash" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Field" TEXT NOT NULL,
    "Value" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Hash_KeyField" ON public."Hash" ("Key", "Field");

-- Table: counter
CREATE TABLE IF NOT EXISTS public."Counter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" BIGINT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Counter_Key" ON public."Counter" ("Key");

-- Table: aggregated_counter
CREATE TABLE IF NOT EXISTS public."AggregatedCounter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" BIGINT NOT NULL,
    "ExpireAt" TIMESTAMP WITHOUT TIME ZONE NULL
);
CREATE INDEX IF NOT EXISTS "IX_AggregatedCounter_Key" ON public."AggregatedCounter" ("Key");

-- Table: lock
CREATE TABLE IF NOT EXISTS public."Lock" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Resource" TEXT NOT NULL,
    "Expiry" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Lock_Resource" ON public."Lock" ("Resource");

-- Table: schema
CREATE TABLE IF NOT EXISTS public."Schema" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Version" INTEGER NOT NULL
);

-- Additional indexes used by Hangfire
CREATE INDEX IF NOT EXISTS "IX_Hash_Key" ON public."Hash" ("Key");
CREATE INDEX IF NOT EXISTS "IX_Job_InvocationData" ON public."Job" ("InvocationData");

-- Note: Depending on the Hangfire.PostgreSql version you use, the exact table names/casing
-- and column names might differ. This script provides a canonical, compatible set of tables
-- that cover the common Hangfire storage entities. If you use a specific Hangfire.PostgreSql
-- version and would like an exact, versioned DDL export, I can fetch the package SQL and
-- produce a precise script matching that version.

-- Finish
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO public;

-- To verify: list tables
-- SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;
