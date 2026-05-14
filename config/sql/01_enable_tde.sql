-- ============================================================================
-- Transparent Data Encryption (TDE) — GymApp database
-- Egypt PDPL Article 8: personal data must be encrypted at rest.
-- Apply AFTER EF Core migrations create the GymApp database.
-- Requires SQL Server Developer or Enterprise edition.
-- ============================================================================

USE [master];
GO

-- 1. Service master key (usually auto-created on SQL Server install, but we
--    force it here so this script is idempotent on a fresh container).
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = N'$(TdeMasterKeyPassword)';
END
GO

-- 2. TDE certificate lives in master.
IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = N'GymAppTdeCert')
BEGIN
    CREATE CERTIFICATE GymAppTdeCert
        WITH SUBJECT = N'GymApp TDE Certificate',
        EXPIRY_DATE = N'2035-12-31';
END
GO

-- Back up the certificate (REQUIRED — without this backup the database cannot
-- be restored on another server). Operator must copy /var/opt/mssql/tde-backup
-- to secure offline storage and remove from the container.
BACKUP CERTIFICATE GymAppTdeCert
    TO FILE = N'/var/opt/mssql/tde-backup/GymAppTdeCert.cer'
    WITH PRIVATE KEY (
        FILE = N'/var/opt/mssql/tde-backup/GymAppTdeCert.pvk',
        ENCRYPTION BY PASSWORD = N'$(TdeCertBackupPassword)'
    );
GO

-- 3. Database encryption key + ENCRYPTION ON.
USE [GymApp];
GO

IF NOT EXISTS (SELECT 1 FROM sys.dm_database_encryption_keys
               WHERE database_id = DB_ID(N'GymApp'))
BEGIN
    CREATE DATABASE ENCRYPTION KEY
        WITH ALGORITHM = AES_256
        ENCRYPTION BY SERVER CERTIFICATE GymAppTdeCert;
END
GO

ALTER DATABASE [GymApp] SET ENCRYPTION ON;
GO

-- 4. Verify (should show encryption_state = 3 == encrypted).
SELECT
    DB_NAME(database_id) AS database_name,
    encryption_state,
    encryption_state_desc = CASE encryption_state
        WHEN 0 THEN 'No encryption key present'
        WHEN 1 THEN 'Unencrypted'
        WHEN 2 THEN 'Encryption in progress'
        WHEN 3 THEN 'Encrypted'
        WHEN 4 THEN 'Key change in progress'
        WHEN 5 THEN 'Decryption in progress'
        WHEN 6 THEN 'Protection change in progress'
    END,
    percent_complete,
    key_algorithm,
    key_length
FROM sys.dm_database_encryption_keys
WHERE database_id = DB_ID(N'GymApp');
GO
