/* ===========================================================================
   Rollback for 2026_05_01_hard_isolation_bootstrap.sql

   Reverses the schema rename: g1_{module} → {module}. Drops gyms_registry
   row for the demo gym. Does NOT remove Owner/Contract rows (those are
   harmless additive data).

   *** RUN IN A MAINTENANCE WINDOW. App must be stopped first and switched
       back to Tenancy:Mode = "Soft" before restart. ***
   =========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @modules TABLE (name SYSNAME);
    INSERT INTO @modules VALUES
        (N'gyms'), (N'training'), (N'progress'), (N'scheduling'),
        (N'nutrition'), (N'communication'), (N'billing'), (N'gamification'),
        (N'outbox');

    DECLARE @module SYSNAME, @oldSchema SYSNAME, @sql NVARCHAR(MAX);

    DECLARE c CURSOR FAST_FORWARD FOR SELECT name FROM @modules;
    OPEN c;
    FETCH NEXT FROM c INTO @module;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @oldSchema = N'g1_' + @module;

        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @module)
            EXEC('CREATE SCHEMA [' + @module + ']');

        SET @sql = N'';
        SELECT @sql = @sql +
               N'ALTER SCHEMA [' + @module + N'] TRANSFER [' + @oldSchema + N'].[' + t.name + N']; '
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name = @oldSchema;

        IF LEN(@sql) > 0 EXEC sp_executesql @sql;

        IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = @oldSchema)
        AND NOT EXISTS (SELECT 1 FROM sys.objects o INNER JOIN sys.schemas s ON o.schema_id = s.schema_id WHERE s.name = @oldSchema)
        BEGIN
            EXEC('DROP SCHEMA [' + @oldSchema + ']');
        END;

        FETCH NEXT FROM c INTO @module;
    END;
    CLOSE c;
    DEALLOCATE c;

    DELETE FROM gyms_registry.gyms;
    DELETE FROM dbo.__tenancy_version;

    COMMIT TRAN;
    PRINT 'Rollback complete. Set Tenancy:Mode = "Soft" and restart the API.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @ErrMsg NVARCHAR(MAX) = ERROR_MESSAGE(),
            @ErrSev INT = ERROR_SEVERITY(),
            @ErrState INT = ERROR_STATE();
    RAISERROR (@ErrMsg, @ErrSev, @ErrState);
END CATCH;
