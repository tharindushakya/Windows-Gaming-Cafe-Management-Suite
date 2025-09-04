-- Create a dedicated hangfire schema and role for Hangfire objects.
-- Run this as a DB admin.

-- Replace these identifiers as needed
-- psql -h <host> -U <admin> -d <db> -f create_hangfire_schema_and_role.sql

DO $$
BEGIN
    -- Create role if it doesn't exist
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'gamingcafe_hangfire') THEN
        CREATE ROLE gamingcafe_hangfire NOLOGIN;
    END IF;
END$$;

CREATE SCHEMA IF NOT EXISTS hangfire AUTHORIZATION gamingcafe_hangfire;

-- Grant usage and privileges on schema to the role
GRANT USAGE ON SCHEMA hangfire TO gamingcafe_hangfire;
GRANT CREATE ON SCHEMA hangfire TO gamingcafe_hangfire;

-- Optionally grant the app's DB user rights on the hangfire schema
-- Replace 'app_user' with your application's DB user
-- GRANT USAGE ON SCHEMA hangfire TO app_user;
-- GRANT CREATE ON SCHEMA hangfire TO app_user;

-- Now create Hangfire tables in the hangfire schema. This is a minimal compatible DDL.
SET search_path TO hangfire;

CREATE TABLE IF NOT EXISTS "Job" (
    "Id" BIGSERIAL PRIMARY KEY,
    "StateId" BIGINT NULL,
    "InvocationData" TEXT NOT NULL,
    "Arguments" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ExpireAt" TIMESTAMP WITHOUT TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS "State" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Reason" TEXT NULL,
    "Data" TEXT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "JobId" BIGINT NOT NULL REFERENCES "Job" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "JobParameter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "JobId" BIGINT NOT NULL REFERENCES "Job" ("Id") ON DELETE CASCADE,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "JobQueue" (
    "Id" BIGSERIAL PRIMARY KEY,
    "JobId" BIGINT NOT NULL REFERENCES "Job" ("Id") ON DELETE CASCADE,
    "Queue" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Server" (
    "Id" TEXT PRIMARY KEY,
    "Heartbeat" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Queues" TEXT NOT NULL,
    "LastSeen" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS "List" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "Set" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "Hash" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Field" TEXT NOT NULL,
    "Value" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "Counter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" BIGINT NOT NULL
);

CREATE TABLE IF NOT EXISTS "AggregatedCounter" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL,
    "Value" BIGINT NOT NULL,
    "ExpireAt" TIMESTAMP WITHOUT TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS "Lock" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Resource" TEXT NOT NULL,
    "Expiry" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS "Schema" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Version" INTEGER NOT NULL
);

-- Grant table privileges to the hangfire role
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA hangfire TO gamingcafe_hangfire;

-- Restore search_path to default
SET search_path TO public;

-- Done
