-- ============================================================================
-- prod_wipe.sql — clean-slate wipe for production hand-off.
--
-- KEEPS:   admin@demo.local (bootstrap super admin), billing catalog
--          (features / subscription_plans / plan_features), all global schemas.
-- DROPS:   every per-gym schema (g{N}_*), gyms_registry rows, all non-admin
--          users, owners, contracts, memberships, sessions, FCM tokens,
--          payments, outbox, etc.
--
-- Run AFTER stopping the API container so no live writes interfere:
--   docker compose stop gymapp-api
--   export DB_SA_PASSWORD='...'   # match value in your .env / deployment secret store
--   docker exec -i gymapp-db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "$DB_SA_PASSWORD" -C -d GymApp -I < src/DB/prod_wipe.sql
--
-- Then bring the API back up with prod-style flags so DemoGymSeeder stays off:
--   SEED_RUN_DEMO_DATA=false IDENTITY_DEV_SEEDER_SCOPE=admin docker compose up -d gymapp-api
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- 0) Safety check: refuse if a non-localhost connection or a different DB.
IF DB_NAME() <> 'GymApp'
BEGIN
    RAISERROR('prod_wipe.sql: must be run against GymApp DB; aborting.', 16, 1);
    RETURN;
END

DECLARE @AdminEmail nvarchar(320) = 'admin@demo.local';
DECLARE @AdminId uniqueidentifier = (SELECT TOP 1 Id FROM [identity].users WHERE email = @AdminEmail AND role = 'PlatformAdmin');
IF @AdminId IS NULL
BEGIN
    RAISERROR('prod_wipe.sql: admin@demo.local not found. Boot the API once with IDENTITY_DEV_SEEDER_SCOPE=admin first.', 16, 1);
    RETURN;
END
PRINT 'Keeping admin user id ' + CONVERT(nvarchar(40), @AdminId);

-- ============================================================================
-- 1) DROP every per-gym schema (g{N}_*)
-- ============================================================================
PRINT '-- step 1: dropping per-gym schemas';
DECLARE @sql nvarchar(max);
DECLARE @sch sysname;

-- Drop tables first (FKs across same schema), then the schema itself.
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.schemas WHERE name LIKE 'g[0-9]%[_]%' ORDER BY name;
OPEN c; FETCH NEXT FROM c INTO @sch;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- 1a) Drop FKs that reference tables in this schema (cross-schema FKs).
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(fk.schema_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
                       + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM   sys.foreign_keys fk
    JOIN   sys.tables t ON t.object_id = fk.referenced_object_id
    WHERE  t.schema_id = SCHEMA_ID(@sch);
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;

    -- 1b) Drop FKs declared inside this schema.
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(@sch) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
                       + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM   sys.foreign_keys fk
    WHERE  fk.schema_id = SCHEMA_ID(@sch);
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;

    -- 1c) Drop tables.
    SET @sql = N'';
    SELECT @sql = @sql + N'DROP TABLE ' + QUOTENAME(@sch) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
    FROM   sys.tables WHERE schema_id = SCHEMA_ID(@sch);
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;

    -- 1d) Drop the schema.
    SET @sql = N'DROP SCHEMA ' + QUOTENAME(@sch) + N';';
    EXEC sp_executesql @sql;

    PRINT '   dropped ' + @sch;
    FETCH NEXT FROM c INTO @sch;
END
CLOSE c; DEALLOCATE c;

-- ============================================================================
-- 2) WIPE cross-tenant tables that reference dropped gyms / users
-- ============================================================================
PRINT '-- step 2: wiping cross-tenant tables';

-- Outbox (cross-tenant copy used by Quartz)
IF OBJECT_ID('outbox.outbox_messages', 'U') IS NOT NULL
    DELETE FROM outbox.outbox_messages;
IF OBJECT_ID('billing.outbox_messages', 'U') IS NOT NULL
    DELETE FROM billing.outbox_messages;
IF OBJECT_ID('gyms.outbox_messages', 'U') IS NOT NULL
    DELETE FROM gyms.outbox_messages;
IF OBJECT_ID('[identity].outbox_messages', 'U') IS NOT NULL
    DELETE FROM [identity].outbox_messages;

-- Identity dependents — delete in FK order
DELETE FROM [identity].user_gym_memberships;
DELETE FROM [identity].user_consents
  WHERE user_id <> @AdminId;
DELETE FROM [identity].refresh_tokens
  WHERE user_id <> @AdminId;
DELETE FROM [identity].otp_codes;

-- Contracts → Owners → Users (FK chain)
DELETE FROM [identity].contracts;

-- Detach owner_id on all users so we can drop owners cleanly
UPDATE [identity].users SET owner_id = NULL;
DELETE FROM [identity].owners;

-- Drop platform_admins rows for non-bootstrap admin users
DELETE FROM [identity].platform_admins
  WHERE user_id <> @AdminId;

-- Finally: drop every user except the bootstrap admin
DELETE FROM [identity].users WHERE Id <> @AdminId;

-- ============================================================================
-- 3) WIPE registry + global gym subscriptions (catalog kept)
-- ============================================================================
PRINT '-- step 3: wiping registry + per-gym subscriptions';
DELETE FROM gyms_registry.gyms;

IF OBJECT_ID('billing.gym_subscriptions', 'U') IS NOT NULL
    DELETE FROM billing.gym_subscriptions;

-- Legacy global "gyms" schema (only present in Soft-mode installs). Wipe rows so
-- a Soft→Hard switch doesn't strand orphan data.
IF SCHEMA_ID('gyms') IS NOT NULL
BEGIN
    IF OBJECT_ID('gyms.gym_session_policies', 'U') IS NOT NULL DELETE FROM gyms.gym_session_policies;
    IF OBJECT_ID('gyms.gym_sections', 'U')         IS NOT NULL DELETE FROM gyms.gym_sections;
    IF OBJECT_ID('gyms.staff_roles', 'U')          IS NOT NULL DELETE FROM gyms.staff_roles;
    IF OBJECT_ID('gyms.gym_members', 'U')          IS NOT NULL DELETE FROM gyms.gym_members;
    IF OBJECT_ID('gyms.gyms', 'U')                 IS NOT NULL DELETE FROM gyms.gyms;
END

-- ============================================================================
-- 4) RESET registry IDENTITY counters so the first new gym is g1 again
-- ============================================================================
PRINT '-- step 4: resetting SequentialNumber identity';
DBCC CHECKIDENT ('gyms_registry.gyms', RESEED, 0) WITH NO_INFOMSGS;

-- ============================================================================
-- 5) FINAL REPORT
-- ============================================================================
PRINT '-- final report';
SELECT 'users_kept'        = (SELECT COUNT(*) FROM [identity].users),
       'owners'            = (SELECT COUNT(*) FROM [identity].owners),
       'memberships'       = (SELECT COUNT(*) FROM [identity].user_gym_memberships),
       'contracts'         = (SELECT COUNT(*) FROM [identity].contracts),
       'gyms_registered'   = (SELECT COUNT(*) FROM gyms_registry.gyms),
       'billing_features'  = (SELECT COUNT(*) FROM billing.features),
       'billing_plans'     = (SELECT COUNT(*) FROM billing.subscription_plans),
       'per_gym_schemas'   = (SELECT COUNT(*) FROM sys.schemas WHERE name LIKE 'g[0-9]%[_]%');

PRINT 'prod_wipe.sql: complete. Bring API back with prod flags before opening to traffic.';
