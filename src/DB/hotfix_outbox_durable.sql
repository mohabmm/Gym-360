-- Hotfix: durable-integration-events outbox backfill (seed §0.6.1, standalone).
--
-- WHY: per-gym module WRITES (add-member enroll, create-announcement, …) 500 on
-- staging because their g{N}_outbox_messages tables predate the durable-events
-- upgrade — the outbox INSERT references a RetryCount column / consumer ledger
-- that doesn't exist yet on those existing schemas. Reads are unaffected, so the
-- API looks healthy until you POST. New gyms already get these from the EF model
-- (GymProvisioningService.CreateTablesAsync); this backfills EXISTING schemas.
--
-- SAFE: fully idempotent (IF COL_LENGTH ... IS NULL / IF OBJECT_ID ... IS NULL).
-- Re-running is a no-op. Excludes the migration-owned 'identity' schema.
--
-- Run against the GymApp database. See scripts/apply_outbox_hotfix.sh for a
-- one-command host runner (executes this inside the gymapp-db container).
SET QUOTED_IDENTIFIER ON;
USE GymApp;

DECLARE @outboxUpgrade nvarchar(max) = N'';
SELECT @outboxUpgrade = @outboxUpgrade + N'
IF COL_LENGTH(' + QUOTENAME(s.name + N'.outbox_messages', '''') + N', ''RetryCount'') IS NULL
    ALTER TABLE ' + QUOTENAME(s.name) + N'.outbox_messages
        ADD RetryCount int NOT NULL CONSTRAINT ' + QUOTENAME(N'DF_' + s.name + N'_outbox_RetryCount') + N' DEFAULT 0;
IF OBJECT_ID(' + QUOTENAME(s.name + N'.outbox_message_consumers', '''') + N', ''U'') IS NULL
    CREATE TABLE ' + QUOTENAME(s.name) + N'.outbox_message_consumers (
        OutboxMessageId uniqueidentifier NOT NULL,
        Name            nvarchar(200)    NOT NULL,
        CONSTRAINT ' + QUOTENAME(N'PK_' + s.name + N'_outbox_message_consumers') + N' PRIMARY KEY (OutboxMessageId, Name)
    );'
FROM sys.schemas s
WHERE OBJECT_ID(s.name + N'.outbox_messages', 'U') IS NOT NULL
  AND s.name <> N'identity';  -- migration-owned; backfilling here breaks its AddColumn migration

IF LEN(@outboxUpgrade) > 0
    EXEC sp_executesql @outboxUpgrade;

-- Report what's now in place (so you can eyeball coverage after running).
SELECT s.name AS [schema],
       COL_LENGTH(s.name + '.outbox_messages', 'RetryCount')                 AS HasRetryCount,
       OBJECT_ID(s.name + '.outbox_message_consumers', 'U')                  AS HasConsumerLedger
FROM sys.schemas s
WHERE OBJECT_ID(s.name + '.outbox_messages', 'U') IS NOT NULL
ORDER BY s.name;
