-- =============================================================================
-- wipe_all_keep_admin.sql
-- Empties every base table in GymApp DB except the platform Super Admin row
-- (identity.users WHERE email = 'admin@demo.local').
--
-- Keeps DDL / schemas / tables intact. Only data wiped.
-- Disables FK constraints, deletes all rows, re-enables FKs (with CHECK).
-- Idempotent.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @AdminEmail NVARCHAR(256) = N'admin@demo.local';
DECLARE @sql NVARCHAR(MAX);

PRINT '--- Step 1: disable all FK constraints ---';
SET @sql = N'';
SELECT @sql = @sql +
    'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) +
    ' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

BEGIN TRAN;

PRINT '--- Step 2: delete data from every table in NON-identity schemas ---';
SET @sql = N'';
SELECT @sql = @sql +
    'DELETE FROM ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(10)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND s.name <> 'identity'
  AND s.name <> 'dbo';                  -- skip dbo (EF migrations history)
EXEC sp_executesql @sql;

PRINT '--- Step 3: clean identity schema (keep only super admin) ---';

-- 3a) wipe identity tables that reference users (or are independent), except users itself
DELETE FROM [identity].refresh_tokens;
DELETE FROM [identity].otp_codes;
DELETE FROM [identity].user_consents;
DELETE FROM [identity].user_gym_memberships;
DELETE FROM [identity].contracts;
DELETE FROM [identity].owners;
DELETE FROM [identity].platform_admins;
DELETE FROM [identity].platform_settings;
DELETE FROM [identity].outbox_messages;

-- 3b) drop owner_id reference on the admin row (if any) so we can clear users
UPDATE [identity].users SET owner_id = NULL WHERE email = @AdminEmail;

-- 3c) delete every user except the admin
DELETE FROM [identity].users WHERE email <> @AdminEmail;

COMMIT TRAN;

PRINT '--- Step 4: re-enable FK constraints WITH CHECK ---';
SET @sql = N'';
SELECT @sql = @sql +
    'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) +
    ' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0;
EXEC sp_executesql @sql;

PRINT '--- Done. Verification ---';
SELECT COUNT(*) AS UsersRemaining FROM [identity].users;
SELECT Id, email, role, status FROM [identity].users;
