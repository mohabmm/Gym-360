/* ===========================================================================
   2026_05_01_hard_isolation_bootstrap.sql

   Migrates the platform from soft multi-tenancy (single shared per-module
   schemas, GymId column-filtered) to hard multi-tenancy (schema-per-Gym,
   prefix g{N}). Existing demo data becomes Owner 1 / Gym 1 under g1_*.

   *** RUN IN A MAINTENANCE WINDOW. App must be stopped before this script
       runs and only restarted with appsettings Tenancy:Mode = "Hard". ***

   Idempotent: safe to re-run. Gates rollout via dbo.__tenancy_version.
   =========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    /* -------------------------------------------------------------------
       0. Version gate. New build refuses to start if version < 1.
       ------------------------------------------------------------------- */
    IF OBJECT_ID('dbo.__tenancy_version', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.__tenancy_version (
            [Version] INT NOT NULL PRIMARY KEY,
            AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            Note NVARCHAR(500) NULL
        );
    END;

    IF EXISTS (SELECT 1 FROM dbo.__tenancy_version WHERE [Version] >= 1)
    BEGIN
        PRINT 'Bootstrap already applied. Exiting.';
        COMMIT TRAN;
        RETURN;
    END;

    /* -------------------------------------------------------------------
       1. Resolve the existing demo owner + demo gym (single-tenant baseline).
       ------------------------------------------------------------------- */
    DECLARE @DemoUserId UNIQUEIDENTIFIER;
    SELECT @DemoUserId = u.Id
    FROM [identity].users u
    WHERE u.email = N'owner@demo.local';

    IF @DemoUserId IS NULL
    BEGIN
        RAISERROR('owner@demo.local not found. Run db/seed.sql first or skip bootstrap on a fresh DB.', 16, 1);
        ROLLBACK TRAN;
        RETURN;
    END;

    DECLARE @DemoGymId UNIQUEIDENTIFIER;
    SELECT TOP 1 @DemoGymId = Id FROM gyms.gyms WHERE OwnerId = @DemoUserId;
    IF @DemoGymId IS NULL
    BEGIN
        RAISERROR('No gym found for demo owner. Cannot bootstrap.', 16, 1);
        ROLLBACK TRAN;
        RETURN;
    END;

    /* -------------------------------------------------------------------
       2. Global schemas (cross-tenant).
       ------------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'gyms_registry')
        EXEC('CREATE SCHEMA [gyms_registry]');

    /* -------------------------------------------------------------------
       3. Owner aggregate + Contract for the existing demo owner.
       Tables created by EF migration AddOwnerContractAndUserOwnerId.
       ------------------------------------------------------------------- */
    DECLARE @OwnerAggregateId UNIQUEIDENTIFIER = NEWID();
    DECLARE @ContractId UNIQUEIDENTIFIER = NEWID();

    IF NOT EXISTS (SELECT 1 FROM [identity].owners WHERE UserId = @DemoUserId)
    BEGIN
        INSERT INTO [identity].owners (Id, UserId, CompanyName, BillingEmail, CreatedAt)
        VALUES (@OwnerAggregateId, @DemoUserId, N'Forma', N'owner@demo.local', SYSUTCDATETIME());
    END
    ELSE
    BEGIN
        SELECT @OwnerAggregateId = Id FROM [identity].owners WHERE UserId = @DemoUserId;
    END;

    IF NOT EXISTS (SELECT 1 FROM [identity].contracts WHERE OwnerId = @OwnerAggregateId)
    BEGIN
        INSERT INTO [identity].contracts (Id, OwnerId, DashboardMode, PlanTier, EffectiveFrom, EffectiveTo)
        VALUES (@ContractId, @OwnerAggregateId, N'Both', N'Pro', SYSUTCDATETIME(), NULL);
    END;

    UPDATE [identity].users SET owner_id = @OwnerAggregateId
    WHERE Id = @DemoUserId AND owner_id IS NULL;

    /* -------------------------------------------------------------------
       4. gyms_registry row for the existing gym → SchemaPrefix g1.
       ------------------------------------------------------------------- */
    IF OBJECT_ID('gyms_registry.gyms', 'U') IS NULL
    BEGIN
        CREATE TABLE gyms_registry.gyms (
            GymId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            OwnerId UNIQUEIDENTIFIER NOT NULL,
            Name NVARCHAR(200) NOT NULL,
            Slug NVARCHAR(100) NOT NULL,
            SchemaPrefix NVARCHAR(20) NOT NULL,
            SequentialNumber INT IDENTITY(1,1) NOT NULL,
            Status NVARCHAR(20) NOT NULL,
            CreatedAt DATETIME2 NOT NULL,
            CONSTRAINT UQ_gyms_registry_gyms_SchemaPrefix UNIQUE (SchemaPrefix),
            CONSTRAINT UQ_gyms_registry_gyms_Slug UNIQUE (Slug),
            CONSTRAINT UQ_gyms_registry_gyms_SequentialNumber UNIQUE (SequentialNumber)
        );
        CREATE INDEX IX_gyms_registry_gyms_OwnerId ON gyms_registry.gyms (OwnerId);
    END;

    IF NOT EXISTS (SELECT 1 FROM gyms_registry.gyms WHERE GymId = @DemoGymId)
    BEGIN
        SET IDENTITY_INSERT gyms_registry.gyms ON;
        INSERT INTO gyms_registry.gyms (GymId, OwnerId, Name, Slug, SchemaPrefix, SequentialNumber, Status, CreatedAt)
        VALUES (@DemoGymId, @OwnerAggregateId, N'Forma', N'forma', N'g1', 1, N'Active', SYSUTCDATETIME());
        SET IDENTITY_INSERT gyms_registry.gyms OFF;
    END;

    /* -------------------------------------------------------------------
       5. Transfer all per-module tables into g1_* schemas.
       Order: create new schemas, then transfer tables, then drop empty
       legacy schemas.
       ------------------------------------------------------------------- */
    DECLARE @modules TABLE (name SYSNAME);
    INSERT INTO @modules VALUES
        (N'gyms'), (N'training'), (N'progress'), (N'scheduling'),
        (N'nutrition'), (N'communication'), (N'billing'), (N'gamification'),
        (N'outbox');

    DECLARE @module SYSNAME;
    DECLARE @newSchema SYSNAME;
    DECLARE @sql NVARCHAR(MAX);

    DECLARE module_cursor CURSOR FAST_FORWARD FOR SELECT name FROM @modules;
    OPEN module_cursor;
    FETCH NEXT FROM module_cursor INTO @module;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @newSchema = N'g1_' + @module;

        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @newSchema)
            EXEC('CREATE SCHEMA [' + @newSchema + ']');

        SET @sql = N'';
        SELECT @sql = @sql +
               N'ALTER SCHEMA [' + @newSchema + N'] TRANSFER [' + @module + N'].[' + t.name + N']; '
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name = @module;

        IF LEN(@sql) > 0 EXEC sp_executesql @sql;

        FETCH NEXT FROM module_cursor INTO @module;
    END;
    CLOSE module_cursor;
    DEALLOCATE module_cursor;

    /* Drop legacy empty schemas (guard against pre-existing objects). */
    DECLARE drop_cursor CURSOR FAST_FORWARD FOR SELECT name FROM @modules;
    OPEN drop_cursor;
    FETCH NEXT FROM drop_cursor INTO @module;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = @module)
        AND NOT EXISTS (
            SELECT 1 FROM sys.objects o INNER JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE s.name = @module
        )
        BEGIN
            EXEC('DROP SCHEMA [' + @module + ']');
        END;

        FETCH NEXT FROM drop_cursor INTO @module;
    END;
    CLOSE drop_cursor;
    DEALLOCATE drop_cursor;

    /* -------------------------------------------------------------------
       6. Stamp version. New build will only boot if Version >= 1.
       ------------------------------------------------------------------- */
    INSERT INTO dbo.__tenancy_version ([Version], Note)
    VALUES (1, N'Soft → Hard isolation bootstrap; existing data became g1_*.');

    COMMIT TRAN;
    PRINT 'Bootstrap complete. Set Tenancy:Mode = "Hard" and restart the API.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @ErrMsg NVARCHAR(MAX) = ERROR_MESSAGE(),
            @ErrSev INT = ERROR_SEVERITY(),
            @ErrState INT = ERROR_STATE();
    RAISERROR (@ErrMsg, @ErrSev, @ErrState);
END CATCH;
