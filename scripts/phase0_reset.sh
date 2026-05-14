#!/usr/bin/env bash
# Phase 0: drop DB, recreate, seed billing reference data via Soft-mode boot,
# then switch to Hard mode for actual operation. Result: empty platform, admin
# user only, billing.* populated so future RegisterGym calls succeed.
# Re-run between loops to reset to clean state. Idempotent.
set -euo pipefail

DB_PASS='GymApp_P@ss2024!'
SQLCMD="docker exec gymapp-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ${DB_PASS} -C"
COMPOSE_DIR="$(cd "$(dirname "$0")"/.. && pwd)"
cd "$COMPOSE_DIR"

wait_for_api() {
  echo ">> wait /health/ready"
  until curl -fsS http://localhost:8080/health/ready > /dev/null 2>&1; do sleep 3; done
  echo ">> API READY"
}

echo ">> Stop API"
docker compose stop gymapp-api

echo ">> Drop+create GymApp DB"
$SQLCMD -Q "IF DB_ID('GymApp') IS NOT NULL BEGIN ALTER DATABASE GymApp SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE GymApp; END; CREATE DATABASE GymApp;"

echo ">> Mini bootstrap: tenancy_version + gyms_registry"
$SQLCMD -d GymApp -Q "
IF OBJECT_ID('dbo.__tenancy_version','U') IS NULL
  CREATE TABLE dbo.__tenancy_version ([Version] INT NOT NULL PRIMARY KEY, AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), Note NVARCHAR(500) NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.__tenancy_version WHERE [Version]=1)
  INSERT INTO dbo.__tenancy_version ([Version], Note) VALUES (1, 'fresh-DB bootstrap');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name=N'gyms_registry') EXEC('CREATE SCHEMA [gyms_registry]');
IF OBJECT_ID('gyms_registry.gyms','U') IS NULL
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
END;"

echo ">> Boot API in Hard mode (code self-bootstraps legacy billing.* via Step 1b)"
TENANCY_MODE=Hard docker compose up -d --force-recreate gymapp-api
wait_for_api

echo ">> Verify final state"
$SQLCMD -d GymApp -Q "SELECT email, role FROM [identity].users; SELECT COUNT(*) AS gyms FROM gyms_registry.gyms; SELECT COUNT(*) AS plans FROM billing.subscription_plans;"
