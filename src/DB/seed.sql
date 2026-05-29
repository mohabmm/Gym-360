-- ============================================================================
-- GymApp Master Seed (db/seed.sql)
--
-- Purpose:
--   Single future-ready seed for the GymApp dev database. Re-runnable
--   (idempotent). Combines infrastructure setup + demo dataset + schema-drift
--   hotfixes for rows/columns the EF migrations don't yet know about.
--
-- Password for every seeded user: 123456
--   BCrypt hash (cost 12) is embedded below. Update CREDENTIALS.md if changed.
--
-- How to run:
--   docker exec -i gymapp-db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "$DB_SA_PASSWORD" -C -d GymApp \
--     -i /path/to/db/seed.sql
--   (or)
--   cat db/seed.sql | docker exec -i gymapp-db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P "$DB_SA_PASSWORD" -C -d GymApp
--
-- When to update:
--   Any time the Identity seeder's baseline users change, or when a new dev
--   migration introduces columns/values the current migrations file doesn't
--   cover. Section 0 (schema-drift hotfixes) should be reviewed first.
-- ============================================================================
SET NOCOUNT ON;
USE GymApp;

-- ============================================================================
-- 0) SCHEMA-DRIFT HOTFIXES
--    Columns / rows the deployed EF migrations don't create but current
--    entity configurations expect. Safe to leave in place indefinitely.
-- ============================================================================

-- 0.1) Bilingual Arabic name columns (entities added NameAr, migrations didn't)
--      Per-tenant schemas live at g{N}_training, scheduling, nutrition. Walk every
--      g% schema and patch its tables; legacy global "training", "scheduling",
--      "nutrition" schemas no longer exist post-multitenancy migration.
DECLARE @sql NVARCHAR(MAX);
DECLARE schema_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.name LIKE 'g[0-9]%_training';
DECLARE @schema SYSNAME;
OPEN schema_cursor;
FETCH NEXT FROM schema_cursor INTO @schema;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'';
    IF OBJECT_ID(QUOTENAME(@schema) + N'.workouts')  IS NOT NULL AND COL_LENGTH(QUOTENAME(@schema) + N'.workouts',  'NameAr') IS NULL
        SET @sql += N'ALTER TABLE ' + QUOTENAME(@schema) + N'.workouts  ADD NameAr NVARCHAR(200) NULL;';
    IF OBJECT_ID(QUOTENAME(@schema) + N'.programs')  IS NOT NULL AND COL_LENGTH(QUOTENAME(@schema) + N'.programs',  'NameAr') IS NULL
        SET @sql += N'ALTER TABLE ' + QUOTENAME(@schema) + N'.programs  ADD NameAr NVARCHAR(200) NULL;';
    IF OBJECT_ID(QUOTENAME(@schema) + N'.exercises') IS NOT NULL AND COL_LENGTH(QUOTENAME(@schema) + N'.exercises', 'NameAr') IS NULL
        SET @sql += N'ALTER TABLE ' + QUOTENAME(@schema) + N'.exercises ADD NameAr NVARCHAR(200) NULL;';
    IF LEN(@sql) > 0 EXEC sp_executesql @sql;
    FETCH NEXT FROM schema_cursor INTO @schema;
END
CLOSE schema_cursor;
DEALLOCATE schema_cursor;

-- 0.2) Legacy 'Scheduled' status → 'Confirmed' (SessionStatus enum has no
--      'Scheduled' member; EF's string-enum converter throws on read).
DECLARE sched_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.name LIKE 'g[0-9]%_scheduling';
DECLARE @sched SYSNAME;
OPEN sched_cursor;
FETCH NEXT FROM sched_cursor INTO @sched;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(QUOTENAME(@sched) + N'.sessions') IS NOT NULL
    BEGIN
        SET @sql = N'UPDATE ' + QUOTENAME(@sched) + N'.sessions SET Status = ''Confirmed'' WHERE Status = ''Scheduled'';';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM sched_cursor INTO @sched;
END
CLOSE sched_cursor;
DEALLOCATE sched_cursor;

-- 0.3) Repair workouts.target_muscle_groups that an earlier seed wrote as a
--      JSON array (e.g. '["chest","back","legs"]'). EF's converter expects a
--      CSV of PascalCase MuscleGroup enum names ('Chest,Back,Legs') and
--      throws on Enum.Parse when it sees a JSON bracket. GET /training/workouts
--      and GET /training/templates both 500 until these are rewritten.
--
--      Note: `[` is a LIKE metacharacter in T-SQL, so the previous pattern
--      `LIKE '[%'` silently matched nothing. Use `LIKE '[[]%'` (character
--      class containing only `[`) to actually detect JSON-prefixed rows.
DECLARE wk_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.name LIKE 'g[0-9]%_training';
DECLARE @wk SYSNAME;
OPEN wk_cursor;
FETCH NEXT FROM wk_cursor INTO @wk;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(QUOTENAME(@wk) + N'.workouts') IS NOT NULL
    BEGIN
        SET @sql = N'UPDATE ' + QUOTENAME(@wk) + N'.workouts SET target_muscle_groups = ''Chest,Back,Legs'' WHERE target_muscle_groups LIKE ''[[]%'';';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM wk_cursor INTO @wk;
END
CLOSE wk_cursor;
DEALLOCATE wk_cursor;

-- 0.4) Repair meal_entries.food_items rows written with the old lowercase/
--      short schema ({"name":...,"grams":...}). EF's OwnsMany.ToJson expects
--      the full FoodItem shape (Name,Quantity,Unit,Calories,Protein,Carbs,Fat);
--      missing fields deserialize as zero and corrupt trainee nutrition views.
DECLARE nut_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.name LIKE 'g[0-9]%_nutrition';
DECLARE @nut SYSNAME;
OPEN nut_cursor;
FETCH NEXT FROM nut_cursor INTO @nut;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(QUOTENAME(@nut) + N'.meal_entries') IS NOT NULL
    BEGIN
        SET @sql = N'
            UPDATE ' + QUOTENAME(@nut) + N'.meal_entries SET food_items = N''[{"Name":"Oats + egg","Quantity":300,"Unit":"g","Calories":450,"Protein":30,"Carbs":45,"Fat":15}]'' WHERE MealType = ''Breakfast'' AND food_items LIKE ''%"grams"%'';
            UPDATE ' + QUOTENAME(@nut) + N'.meal_entries SET food_items = N''[{"Name":"Chicken + rice","Quantity":400,"Unit":"g","Calories":700,"Protein":55,"Carbs":70,"Fat":25}]'' WHERE MealType = ''Lunch'' AND food_items LIKE ''%"grams"%'';
            UPDATE ' + QUOTENAME(@nut) + N'.meal_entries SET food_items = N''[{"Name":"Fish + salad","Quantity":350,"Unit":"g","Calories":700,"Protein":50,"Carbs":65,"Fat":20}]'' WHERE MealType = ''Dinner'' AND food_items LIKE ''%"grams"%'';';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM nut_cursor INTO @nut;
END
CLOSE nut_cursor;
DEALLOCATE nut_cursor;

-- 0.4a) food_logs unique index must include GymId so a multi-gym trainee can
--       have one daily log per gym. Original migration created the unique
--       index on (UserId, Date) only — POST /nutrition/food-log 500s with a
--       duplicate-key error when the same user already has a row for that
--       date under a different gym. Rename + widen the index; drop the now-
--       redundant non-unique (UserId, GymId) prefix index.
IF OBJECT_ID('nutrition.food_logs') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_food_logs_user_date'
                                           AND object_id = OBJECT_ID('nutrition.food_logs'))
        DROP INDEX IX_food_logs_user_date ON nutrition.food_logs;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_food_logs_user_gym'
                                           AND object_id = OBJECT_ID('nutrition.food_logs'))
        DROP INDEX IX_food_logs_user_gym ON nutrition.food_logs;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_food_logs_user_gym_date'
                                               AND object_id = OBJECT_ID('nutrition.food_logs'))
        CREATE UNIQUE INDEX IX_food_logs_user_gym_date
            ON nutrition.food_logs (UserId, GymId, Date);
END

-- 0.5) Enum-value repairs. Earlier seed runs wrote values that don't exist in
--      the domain enums; EF's string-enum converter throws on Enum.Parse.
--      These repair any row still holding a non-member value, so re-running
--      seed.sql brings the DB back to a readable state without a full reset.

-- identity.users.language: 'ar'/'en' → Arabic/English (enum: Arabic, English)
UPDATE [identity].users
SET    language = 'Arabic'
WHERE  language IN ('ar', 'AR', 'Ar');
UPDATE [identity].users
SET    language = 'English'
WHERE  language IN ('en', 'EN', 'En');

-- gyms.gym_session_policies.NoshowAction: 'Warn' (legacy) → FlagOnly
IF OBJECT_ID('gyms.gym_session_policies') IS NOT NULL
    UPDATE gyms.gym_session_policies
    SET    NoshowAction = 'FlagOnly'
    WHERE  NoshowAction NOT IN ('FlagOnly','NotifyTrainer','SuspendBooking');

-- 0.X-pre) Bio + Specializations columns on every per-gym gym_trainer_requests
--          table (entity GymTrainerRequest gained Bio/Specializations after the
--          AddGymTrainerRequestsAndMemberFreeze migration shipped). Multitenancy
--          rewrites schema to g{N}_gyms at runtime so plain dotnet ef cannot
--          target every tenant — apply via dynamic SQL instead.
DECLARE @gymTrainerRequestsAlter NVARCHAR(MAX) = N'';
SELECT @gymTrainerRequestsAlter = @gymTrainerRequestsAlter +
       N'IF COL_LENGTH(''' + s.name + N'.gym_trainer_requests'',''Bio'') IS NULL ' +
       N'ALTER TABLE [' + s.name + N'].gym_trainer_requests ADD Bio NVARCHAR(2000) NULL;' + CHAR(10) +
       N'IF COL_LENGTH(''' + s.name + N'.gym_trainer_requests'',''Specializations'') IS NULL ' +
       N'ALTER TABLE [' + s.name + N'].gym_trainer_requests ADD Specializations NVARCHAR(MAX) NOT NULL ' +
       N'CONSTRAINT DF_' + s.name + N'_gym_trainer_requests_Specializations DEFAULT(''[]'');' + CHAR(10)
FROM   sys.schemas s
JOIN   sys.tables  t ON t.schema_id = s.schema_id
WHERE  t.name = 'gym_trainer_requests';
IF LEN(@gymTrainerRequestsAlter) > 0 EXEC sp_executesql @gymTrainerRequestsAlter;

-- 0.X) gyms.gym_members table — created by migration 20260424000000_AddGymMembers.
--      Defensive CREATE IF NOT EXISTS so seed.sql works even if migrations
--      haven't run yet (e.g. fresh checkout seeded before first app boot).
--      Skips entirely under hard multitenancy (per-gym schemas g{N}_gyms only).
IF SCHEMA_ID('gyms') IS NOT NULL AND OBJECT_ID('gyms.gym_members', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE gyms.gym_members (
        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_gym_members PRIMARY KEY,
        GymId         UNIQUEIDENTIFIER NOT NULL,
        MemberId      UNIQUEIDENTIFIER NOT NULL,
        MemberNumber  NVARCHAR(40)     NOT NULL,
        Section       NVARCHAR(60)     NULL,
        JoinedAt      DATETIME2        NOT NULL
    );
    CREATE UNIQUE INDEX IX_gym_members_GymId_MemberId     ON gyms.gym_members (GymId, MemberId);
    CREATE UNIQUE INDEX IX_gym_members_GymId_MemberNumber ON gyms.gym_members (GymId, MemberNumber);');
END

-- billing.payments.Status: 'Paid' (legacy) → Completed
IF OBJECT_ID('billing.payments') IS NOT NULL
    UPDATE billing.payments
    SET    Status = 'Completed'
    WHERE  Status = 'Paid';

-- training.workout_assignments.AssignmentType: 'SingleWorkout' (legacy) → Workout
IF OBJECT_ID('training.workout_assignments') IS NOT NULL
    UPDATE training.workout_assignments
    SET    AssignmentType = 'Workout'
    WHERE  AssignmentType IN ('SingleWorkout','Single');

-- gyms.announcements.TargetAudience: 'AllMembers' (legacy) → Members
IF OBJECT_ID('gyms.announcements') IS NOT NULL
BEGIN
    UPDATE gyms.announcements
    SET    TargetAudience = 'Members'
    WHERE  TargetAudience = 'AllMembers';
    UPDATE gyms.announcements
    SET    TargetAudience = 'All'
    WHERE  TargetAudience IN ('Everyone','Everybody');
END

-- progress.body_metric_entries.Source: 'Self'/'Trainer' (legacy) → SelfReported/TrainerMeasured
IF OBJECT_ID('progress.body_metric_entries') IS NOT NULL
BEGIN
    UPDATE progress.body_metric_entries
    SET    Source = 'SelfReported'
    WHERE  Source = 'Self';
    UPDATE progress.body_metric_entries
    SET    Source = 'TrainerMeasured'
    WHERE  Source = 'Trainer';
END

-- progress.streaks.StreakType: 'WorkoutLogging' (legacy) → DailyWorkout
IF OBJECT_ID('progress.streaks') IS NOT NULL
BEGIN
    UPDATE progress.streaks
    SET    StreakType = 'DailyWorkout'
    WHERE  StreakType IN ('WorkoutLogging','Daily');
    UPDATE progress.streaks
    SET    StreakType = 'WeeklyWorkout'
    WHERE  StreakType = 'Weekly';
    UPDATE progress.streaks
    SET    StreakType = 'DailyCheckIn'
    WHERE  StreakType IN ('CheckIn','GymVisit');
END

-- 0.6) Create per-schema outbox_messages tables for modules whose EF migrations
--      predate the outbox wiring. ConvertDomainEventsToOutboxMessagesInterceptor
--      calls dbContext.Set<OutboxMessage>() on every SaveChanges, so any module
--      that raises domain events (Training.Workout, Billing.*, Gamification.*,
--      Communication.*) 500s until these tables exist. Mirrors the schema EF
--      generated for progress/gyms/identity/scheduling/nutrition.
--      QUOTED_IDENTIFIER must be ON for filtered indexes (SQL Server requirement).
SET QUOTED_IDENTIFIER ON;

IF SCHEMA_ID('training') IS NOT NULL AND OBJECT_ID('training.outbox_messages', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE training.outbox_messages (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_training_outbox_messages PRIMARY KEY,
        OccurredOnUtc   datetime2        NOT NULL,
        Type            nvarchar(500)    NOT NULL,
        Content         nvarchar(max)    NOT NULL,
        ProcessedOnUtc  datetime2        NULL,
        Error           nvarchar(max)    NULL
    );
    CREATE INDEX IX_training_outbox_messages_ProcessedOnUtc
        ON training.outbox_messages (ProcessedOnUtc)
        WHERE ProcessedOnUtc IS NULL;');
END

IF SCHEMA_ID('billing') IS NOT NULL AND OBJECT_ID('billing.outbox_messages', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE billing.outbox_messages (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_billing_outbox_messages PRIMARY KEY,
        OccurredOnUtc   datetime2        NOT NULL,
        Type            nvarchar(500)    NOT NULL,
        Content         nvarchar(max)    NOT NULL,
        ProcessedOnUtc  datetime2        NULL,
        Error           nvarchar(max)    NULL
    );
    CREATE INDEX IX_billing_outbox_messages_ProcessedOnUtc
        ON billing.outbox_messages (ProcessedOnUtc)
        WHERE ProcessedOnUtc IS NULL;');
END

IF SCHEMA_ID('gamification') IS NOT NULL AND OBJECT_ID('gamification.outbox_messages', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE gamification.outbox_messages (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_gamification_outbox_messages PRIMARY KEY,
        OccurredOnUtc   datetime2        NOT NULL,
        Type            nvarchar(500)    NOT NULL,
        Content         nvarchar(max)    NOT NULL,
        ProcessedOnUtc  datetime2        NULL,
        Error           nvarchar(max)    NULL
    );
    CREATE INDEX IX_gamification_outbox_messages_ProcessedOnUtc
        ON gamification.outbox_messages (ProcessedOnUtc)
        WHERE ProcessedOnUtc IS NULL;');
END

IF SCHEMA_ID('communication') IS NOT NULL AND OBJECT_ID('communication.outbox_messages', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE communication.outbox_messages (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_communication_outbox_messages PRIMARY KEY,
        OccurredOnUtc   datetime2        NOT NULL,
        Type            nvarchar(500)    NOT NULL,
        Content         nvarchar(max)    NOT NULL,
        ProcessedOnUtc  datetime2        NULL,
        Error           nvarchar(max)    NULL
    );
    CREATE INDEX IX_communication_outbox_messages_ProcessedOnUtc
        ON communication.outbox_messages (ProcessedOnUtc)
        WHERE ProcessedOnUtc IS NULL;');
END

-- 0.6.1) Durable integration-events upgrade. Backfill EVERY existing outbox_messages
--        schema (soft 'outbox', legacy per-module schemas, and every per-gym
--        g{N}_outbox) with:
--          * RetryCount column — the outbox processor increments it on handler
--            failure and dead-letters at OutboxOptions.MaxRetryCount instead of
--            stamping ProcessedOnUtc immediately (old behaviour = at-most-once).
--          * outbox_message_consumers ledger — (OutboxMessageId, Name) records which
--            handlers already ran, so a retried message only re-invokes the handler(s)
--            that failed (keeps at-least-once delivery idempotent).
--        New gyms get both from the EF model via GymProvisioningService.CreateTablesAsync;
--        this is the existing-schema backfill. Schema-agnostic + idempotent.
SET QUOTED_IDENTIFIER ON;
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
WHERE OBJECT_ID(s.name + N'.outbox_messages', 'U') IS NOT NULL;

IF LEN(@outboxUpgrade) > 0
    EXEC sp_executesql @outboxUpgrade;

-- 0.7) Create gyms.data_import_jobs (entity exists; migration never created the
--      table). GET /gyms/{id}/imports, GET /gyms/{id}/imports/{jobId}, and
--      GET /gyms/{id}/imports/{jobId}/errors.csv 500 with "Invalid object name"
--      until this table exists. Mirrors DataImportJobConfiguration.cs.
IF SCHEMA_ID('gyms') IS NOT NULL AND OBJECT_ID('gyms.data_import_jobs', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE gyms.data_import_jobs (
        Id                  uniqueidentifier NOT NULL CONSTRAINT PK_gyms_data_import_jobs PRIMARY KEY,
        GymId               uniqueidentifier NOT NULL,
        UploadedByUserId    uniqueidentifier NOT NULL,
        Source              int              NOT NULL,
        Status              int              NOT NULL,
        OriginalFileName    nvarchar(260)    NOT NULL,
        StoragePath         nvarchar(500)    NOT NULL,
        FileSizeBytes       bigint           NOT NULL,
        TotalRowCount       int              NOT NULL,
        SuccessCount        int              NOT NULL,
        FailureCount        int              NOT NULL,
        ErrorReportPath     nvarchar(500)    NULL,
        FailureReason       nvarchar(1000)   NULL,
        CreatedAt           datetime2        NOT NULL,
        StartedAt           datetime2        NULL,
        CompletedAt         datetime2        NULL
    );
    CREATE INDEX IX_gyms_data_import_jobs_GymId_CreatedAt
        ON gyms.data_import_jobs (GymId, CreatedAt);
    CREATE INDEX IX_gyms_data_import_jobs_Status
        ON gyms.data_import_jobs (Status);');
END

-- 0.8) Create gyms.member_notes (MemberNote entity, migration never shipped).
IF SCHEMA_ID('gyms') IS NOT NULL AND OBJECT_ID('gyms.member_notes', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE gyms.member_notes (
        Id          uniqueidentifier NOT NULL CONSTRAINT PK_gyms_member_notes PRIMARY KEY,
        GymId       uniqueidentifier NOT NULL,
        MemberId    uniqueidentifier NOT NULL,
        CreatedBy   uniqueidentifier NOT NULL,
        Content     nvarchar(2000)   NOT NULL,
        CreatedAt   datetime2        NOT NULL
    );
    CREATE INDEX IX_gyms_member_notes_GymId_MemberId
        ON gyms.member_notes (GymId, MemberId);');
END

-- 0.9) Create gyms.member_onboardings (MemberOnboarding entity, missing table).
IF SCHEMA_ID('gyms') IS NOT NULL AND OBJECT_ID('gyms.member_onboardings', 'U') IS NULL
BEGIN
    EXEC('CREATE TABLE gyms.member_onboardings (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_gyms_member_onboardings PRIMARY KEY,
        GymId           uniqueidentifier NOT NULL,
        MemberId        uniqueidentifier NOT NULL,
        MedicalHistory  nvarchar(2000)   NULL,
        FitnessGoals    nvarchar(2000)   NULL,
        Injuries        nvarchar(2000)   NULL,
        Medications     nvarchar(2000)   NULL,
        FitnessLevel    nvarchar(50)     NULL
    );
    CREATE UNIQUE INDEX IX_gyms_member_onboardings_GymId_MemberId
        ON gyms.member_onboardings (GymId, MemberId);');
END

-- 0.10) session_exercise_logs (SessionExerciseLog entity, AddSessionExerciseLogs
--       migration ships in src/Modules/Scheduling/.../Migrations but per-gym
--       schemas were created via IRelationalDatabaseCreator.CreateTablesAsync,
--       not by running migrations. Walk every scheduling-bearing schema and
--       create the table where it is missing. Idempotent.
DECLARE @selSql NVARCHAR(MAX) = N'';
SELECT @selSql = @selSql +
       N'IF OBJECT_ID(''' + s.name + N'.session_exercise_logs'', ''U'') IS NULL ' +
       N'EXEC(''CREATE TABLE [' + s.name + N'].session_exercise_logs (' +
       N'Id uniqueidentifier NOT NULL CONSTRAINT PK_' + s.name + N'_session_exercise_logs PRIMARY KEY, ' +
       N'SessionId uniqueidentifier NOT NULL, ' +
       N'ExerciseId uniqueidentifier NULL, ' +
       N'ExerciseName nvarchar(200) NOT NULL, ' +
       N'ExerciseNameAr nvarchar(200) NULL, ' +
       N'OrderIndex int NOT NULL, ' +
       N'Sets int NOT NULL, ' +
       N'Reps nvarchar(50) NOT NULL, ' +
       N'Weight decimal(8,2) NULL, ' +
       N'Notes nvarchar(1000) NULL, ' +
       N'CreatedAt datetime2 NOT NULL, ' +
       N'UpdatedAt datetime2 NULL, ' +
       N'CONSTRAINT FK_' + s.name + N'_session_exercise_logs_sessions FOREIGN KEY (SessionId) ' +
       N'REFERENCES [' + s.name + N'].sessions (Id) ON DELETE CASCADE); ' +
       N'CREATE INDEX IX_session_exercise_logs_session_order ON [' + s.name + N'].session_exercise_logs (SessionId, OrderIndex);'');' + CHAR(10)
FROM   sys.schemas s
JOIN   sys.tables  t ON t.schema_id = s.schema_id
WHERE  t.name = 'sessions'
  AND  (s.name = 'scheduling' OR s.name LIKE 'g[0-9]%_scheduling');
IF LEN(@selSql) > 0 EXEC sp_executesql @selSql;

-- 0.11) Add peak_hours_analytics feature to every per-gym billing schema.
--       Handler GetPeakHoursAnalyticsQueryHandler.FeatureCode = "peak_hours_analytics"
--       was added 2026-05-09 but the per-gym `g{N}_billing.features` catalogs were
--       provisioned earlier and never include it, so /analytics/peak-hours returns
--       PLAN.FEATURE_NOT_AVAILABLE on every existing tenant. Idempotent.
DECLARE phf_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name FROM sys.schemas s WHERE s.name LIKE 'g[0-9]%_billing';
DECLARE @phf SYSNAME;
OPEN phf_cursor;
FETCH NEXT FROM phf_cursor INTO @phf;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'
        SET QUOTED_IDENTIFIER ON;
        IF OBJECT_ID(''' + @phf + N'.features'') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM ' + QUOTENAME(@phf) + N'.features WHERE Code=''peak_hours_analytics'')
        BEGIN
          DECLARE @FId uniqueidentifier = NEWID();
          INSERT INTO ' + QUOTENAME(@phf) + N'.features (Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt)
          VALUES (@FId, ''peak_hours_analytics'', ''Peak Hours Analytics'',
                  ''Check-ins by hour and weekday over a date range'', ''Analytics'',
                  1, 135, SYSUTCDATETIME());
          INSERT INTO ' + QUOTENAME(@phf) + N'.plan_features (Id, PlanId, FeatureId, IsEnabled, LimitValue)
          SELECT NEWID(), p.Id, @FId, 1, NULL
          FROM ' + QUOTENAME(@phf) + N'.subscription_plans p
          WHERE p.Code IN (''pro'', ''business'');
        END';
    EXEC sp_executesql @sql;
    FETCH NEXT FROM phf_cursor INTO @phf;
END
CLOSE phf_cursor;
DEALLOCATE phf_cursor;

-- ============================================================================
-- 1) CONSTANTS
-- ============================================================================
-- DemoGymSeeder creates the gym with Guid.NewGuid() on API boot, so we must
-- resolve @GymId at runtime from the owner's email rather than hardcoding it.
-- If the row doesn't exist yet (API never booted), we create it here so the
-- trainer app has an active gym on first `flutter run`.
DECLARE @Now    datetime2        = SYSUTCDATETIME();
DECLARE @Today  date             = CAST(@Now AS date);

-- BCrypt hash of '123456' (cost 12). Keep in sync with CREDENTIALS.md §1.
DECLARE @Hash   nvarchar(256)    = '$2a$12$XvPCzAgvugKrHNYw.OiA0.puVgft9WxQA0Ar.jon7ZjnLP9fzhKRO';

-- @GymId is bound after section 6 once owner@demo.local is known to exist.
DECLARE @GymId  uniqueidentifier;

-- ============================================================================
-- 2) FEATURES (billing.features)
--    17 feature codes referenced by handlers in src/. Idempotent insert.
-- ============================================================================
DECLARE @features TABLE (Code nvarchar(100), Name nvarchar(200), Category nvarchar(50), SortOrder int);
INSERT INTO @features VALUES
 ('session_logging',         'Session logging',          'training',      10),
 ('workout_builder',         'Workout builder',          'training',      20),
 ('multi_week_programs',     'Multi-week programs',      'training',      30),
 ('custom_exercise_library', 'Custom exercise library',  'training',      40),
 ('remote_workout_mode',     'Remote workout mode',      'training',      50),
 ('progress_tracking',       'Progress tracking',        'progress',      60),
 ('progress_photos',         'Progress photos',          'progress',      70),
 ('nutrition_tracking',      'Nutrition tracking',       'nutrition',     80),
 ('meal_plans',              'Meal plans',               'nutrition',     90),
 ('class_management',        'Class management',         'scheduling',   100),
 ('session_packages',        'Session packages',         'billing',      110),
 ('payment_recording',       'Payment recording',        'billing',      120),
 ('gym_branding',            'Gym branding',             'gyms',         130),
 ('peak_hours_analytics',    'Peak hours analytics',     'gyms',         135),
 ('member_leaderboards',     'Member leaderboards',      'gamification', 140),
 ('trainer_ratings',         'Trainer ratings',          'gamification', 150),
 ('coach_of_the_month',      'Coach of the month',       'gamification', 160),
 ('testimonial_moderation',  'Testimonial moderation',   'gamification', 170),
 ('data_import',             'Data import',              'gyms',         180);

INSERT INTO billing.features (Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt)
SELECT NEWID(), f.Code, f.Name, f.Name, f.Category, 1, f.SortOrder, @Now
FROM @features f
WHERE NOT EXISTS (SELECT 1 FROM billing.features bf WHERE bf.Code = f.Code);

-- ============================================================================
-- 3) DEMO PRO PLAN (billing.subscription_plans) + plan_features (all ON)
-- ============================================================================
DECLARE @PlanId uniqueidentifier;
SELECT @PlanId = Id FROM billing.subscription_plans WHERE Code = 'demo-pro';
IF @PlanId IS NULL
BEGIN
    SET @PlanId = NEWID();
    INSERT INTO billing.subscription_plans
     (Id, Code, Name, Description, MonthlyPriceEgp, AnnualPriceEgp,
      MemberLimit, TrainerLimit, StaffLimit, IsActive, IsPublic, IsTimeLimited,
      SortOrder, CreatedAt)
    VALUES
     (@PlanId, 'demo-pro', 'Demo Pro', 'Seeded demo plan with all features',
      999.00, 9999.00, 500, 20, 10, 1, 1, 0, 1, @Now);
END

INSERT INTO billing.plan_features (Id, PlanId, FeatureId, IsEnabled, LimitValue)
SELECT NEWID(), @PlanId, f.Id, 1, NULL
FROM billing.features f
WHERE NOT EXISTS (
    SELECT 1 FROM billing.plan_features pf
    WHERE pf.PlanId = @PlanId AND pf.FeatureId = f.Id);

-- Ensure ALL features are enabled on demo-pro (idempotent — fixes rows added
-- with IsEnabled=0 by test runs before this plan existed).
UPDATE billing.plan_features
SET    IsEnabled = 1
WHERE  PlanId = @PlanId AND IsEnabled = 0;

-- ============================================================================
-- 3.5) RESOLVE DEMO GYM
--      DemoGymSeeder.cs creates the gym with Guid.NewGuid() at API boot, so
--      @GymId must be resolved from the actual row, not hardcoded. If the gym
--      row is missing (seed.sql run before API boot), we stop — Section 4+
--      requires a valid gym to attach to.
-- ============================================================================
DECLARE @DemoOwnerId uniqueidentifier =
    (SELECT TOP 1 Id FROM [identity].users WHERE email = 'owner@demo.local');

-- Under hard multitenancy the legacy global "gyms" schema is gone (per-gym
-- g{N}_gyms exists instead). Skip the rest of the seed in that case; only
-- Section 6 (identity password reset) is needed for hard-isolation installs.
IF SCHEMA_ID('gyms') IS NULL
BEGIN
    PRINT 'INFO: hard-multitenancy detected (no global gyms schema). Skipping legacy demo data seed.';
    GOTO SkipLegacyDemoData;
END

SELECT TOP 1 @GymId = Id
FROM   gyms.gyms
WHERE  OwnerId = @DemoOwnerId
ORDER  BY CreatedAt;

IF @GymId IS NULL
BEGIN
    PRINT 'ERROR: demo gym not found for owner@demo.local. Boot the API first so DemoGymSeeder runs, then re-run seed.sql.';
    RETURN;
END

-- Guarantee the gym is Active (DemoGymSeeder approves it, but a test reset
-- may have flipped it to Suspended / PendingApproval). The trainer app hides
-- non-active gyms from the gym switcher and X-Gym-Id resolver returns null,
-- which is exactly the "no active gym" failure the exercise form was hitting.
UPDATE gyms.gyms SET Status = 'Active', UpdatedAt = @Now
WHERE Id = @GymId AND Status <> 'Active';

-- Rename the demo gym to "FirstGym" for installs that were created before the
-- DemoGymSeeder rename (previously "نادي فورما الرياضي"). Idempotent.
UPDATE gyms.gyms SET Name = 'FirstGym', UpdatedAt = @Now
WHERE Id = @GymId AND Name <> 'FirstGym';

-- ============================================================================
-- 4) GYM SUBSCRIPTION: demo gym → demo-pro (active, paid, 1-year)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM billing.gym_subscriptions WHERE GymId = @GymId AND IsActive = 1)
    INSERT INTO billing.gym_subscriptions
     (Id, GymId, PlanId, StartedAt, ExpiresAt, IsActive, IsTrial, PaymentStatus,
      LastPaymentAt, NextPaymentDue)
    VALUES
     (NEWID(), @GymId, @PlanId, @Now, DATEADD(YEAR, 1, @Now), 1, 0, 'Paid',
      @Now, DATEADD(MONTH, 1, @Now));
ELSE
    -- Ensure existing subscription points to demo-pro and hasn't expired
    UPDATE billing.gym_subscriptions
    SET PlanId = @PlanId,
        ExpiresAt = DATEADD(YEAR, 1, @Now),
        PaymentStatus = 'Paid'
    WHERE GymId = @GymId AND IsActive = 1;

-- ============================================================================
-- 5) GYM SECTION + SESSION POLICY
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM gyms.gym_sections WHERE GymId = @GymId)
    INSERT INTO gyms.gym_sections (Id, GymId, SectionType, Name, IsActive)
    VALUES (NEWID(), @GymId, 'Mixed', 'Main Floor', 1);

IF NOT EXISTS (SELECT 1 FROM gyms.gym_session_policies WHERE GymId = @GymId)
    INSERT INTO gyms.gym_session_policies
     (Id, GymId, CancellationWindowHours, NoshowThreshold, NoshowWindowDays,
      NoshowAction, UpdatedAt)
    VALUES
     (NEWID(), @GymId, 24, 3, 30, 'FlagOnly', @Now);

SkipLegacyDemoData:

-- ============================================================================
-- 6) DEMO USERS — reset passwords to 123456 and activate
-- ============================================================================
UPDATE [identity].users
SET password_hash = @Hash,
    status        = 'Active',
    updated_at    = @Now
WHERE email IN ('admin@demo.local','owner@demo.local','staff@demo.local',
                'trainer@demo.local','trainee@demo.local');

DECLARE @AdminId   uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='admin@demo.local');
DECLARE @OwnerId   uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='owner@demo.local');
DECLARE @StaffId   uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='staff@demo.local');
DECLARE @TrainerId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainer@demo.local');
DECLARE @TraineeId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainee@demo.local');

-- Extra trainees for richer demo
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='trainee2@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'trainee2@demo.local','+201000000012',@Hash,'Nour','Ali',
            NULL, NULL, 'Female', 'Trainee', 'Active','Arabic', @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='trainee3@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'trainee3@demo.local','+201000000013',@Hash,'Omar','Hassan',
            NULL, NULL, 'Male', 'Trainee', 'Active','Arabic', @Now, @Now);

DECLARE @Trainee2Id uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainee2@demo.local');
DECLARE @Trainee3Id uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainee3@demo.local');

-- Sections 7-12 attach demo data to the legacy global gyms schema. Skip them
-- entirely under hard multitenancy; the user/membership wiring there assumes
-- shared-tenancy tables that do not exist in g{N}_* mode.
IF SCHEMA_ID('gyms') IS NULL OR @GymId IS NULL
BEGIN
    PRINT 'INFO: hard-multitenancy or no demo gym; skipping legacy demo memberships/staff/PT data.';
    GOTO EndOfSeed;
END

-- ============================================================================
-- 7) MEMBERSHIPS — attach every demo user to Forma
-- ============================================================================
DECLARE @seedMembers TABLE (Email nvarchar(320), MembRole nvarchar(40),
                            Section nvarchar(20), MemberNum nvarchar(50));
INSERT INTO @seedMembers VALUES
 ('owner@demo.local',    'GymOwner', NULL,    NULL),
 ('staff@demo.local',    'GymStaff', NULL,    NULL),
 ('trainer@demo.local',  'Trainer',  NULL,    NULL),
 ('trainee@demo.local',  'Trainee',  'Mixed', 'M-0001'),
 ('trainee2@demo.local', 'Trainee',  'Mixed', 'M-0002'),
 ('trainee3@demo.local', 'Trainee',  'Mixed', 'M-0003');

INSERT INTO [identity].user_gym_memberships
 (gym_id, role, joined_at, is_active, section, member_number, UserId)
SELECT @GymId, sm.MembRole, @Now, 1, sm.Section, sm.MemberNum, u.Id
FROM @seedMembers sm
JOIN [identity].users u ON u.email = sm.Email
WHERE NOT EXISTS (
    SELECT 1 FROM [identity].user_gym_memberships m
    WHERE m.UserId = u.Id AND m.gym_id = @GymId);

-- Ensure canonical trainee has the right member_number/section if pre-existing
UPDATE [identity].user_gym_memberships
SET member_number='M-0001', section='Mixed'
WHERE UserId=@TraineeId AND gym_id=@GymId AND (member_number IS NULL OR section IS NULL);

-- Re-activate the trainer's membership and user account if a prior test run
-- deactivated either. The trainer app resolves "active gym" from the first
-- membership with is_active=1; without this, login succeeds but X-Gym-Id
-- stays null and POST /training/exercises fails client-side with
-- "no authenticated trainer or active gym".
UPDATE [identity].user_gym_memberships
SET    is_active = 1, role = 'Trainer'
WHERE  UserId = @TrainerId AND gym_id = @GymId AND (is_active = 0 OR role <> 'Trainer');

UPDATE [identity].users
SET    status = 'Active', updated_at = @Now
WHERE  Id = @TrainerId AND status <> 'Active';

-- ============================================================================
-- 8) PDPL CONSENTS (Egypt PDPL) — DataProcessing + Marketing for all users
-- ============================================================================
DECLARE @users TABLE (UserId uniqueidentifier);
INSERT INTO @users VALUES (@AdminId),(@OwnerId),(@StaffId),(@TrainerId),
                          (@TraineeId),(@Trainee2Id),(@Trainee3Id);

INSERT INTO [identity].user_consents
 (Id,user_id,consent_type,is_granted,granted_at,withdrawn_at,consent_text,ip_address)
SELECT NEWID(), u.UserId, ct.ConsentType, 1, @Now, NULL, 'Demo seed consent', '127.0.0.1'
FROM @users u
CROSS JOIN (VALUES ('DataProcessing'),('Marketing')) ct(ConsentType)
WHERE NOT EXISTS (SELECT 1 FROM [identity].user_consents c
                  WHERE c.user_id=u.UserId AND c.consent_type=ct.ConsentType);

-- ============================================================================
-- 9) STAFF ROLE + TRAINER RATE + TRAINER PUBLIC PROFILE
-- ============================================================================
-- gyms.staff_roles.Role maps to StaffRoleType enum {Owner, Manager, Receptionist}.
-- 'GymStaff' is the *membership* role, not a staff-role enum member — using it
-- here crashes EF's enum converter when GET /gyms/{id}/staff materialises rows.
IF NOT EXISTS (SELECT 1 FROM gyms.staff_roles WHERE GymId=@GymId AND UserId=@StaffId)
    INSERT INTO gyms.staff_roles (Id,GymId,UserId,Role)
    VALUES (NEWID(),@GymId,@StaffId,'Receptionist');

-- Repair any pre-existing row that still holds the legacy 'GymStaff' value
UPDATE gyms.staff_roles
SET    Role = 'Receptionist'
WHERE  Role NOT IN ('Owner','Manager','Receptionist');

IF NOT EXISTS (SELECT 1 FROM gyms.trainer_rates WHERE GymId=@GymId AND TrainerId=@TrainerId)
    INSERT INTO gyms.trainer_rates (Id,GymId,TrainerId,RatePerSession,Currency,EffectiveFrom,SetBy)
    VALUES (NEWID(),@GymId,@TrainerId,300.00,'EGP',@Now,@OwnerId);

-- Seed with DisplayRating = 0 so the UI correctly shows the "No ratings yet"
-- empty state. Previously we seeded 4.8 which made the trainer home show a
-- phantom "4.8★ with 0 تقييم" since no individual ratings exist.
IF NOT EXISTS (SELECT 1 FROM gamification.trainer_public_profiles
               WHERE TrainerId=@TrainerId AND GymId=@GymId)
    INSERT INTO gamification.trainer_public_profiles
     (Id,TrainerId,GymId,DisplayRating,CoachOfTheMonthCount,UpdatedAt)
    VALUES (NEWID(),@TrainerId,@GymId,0,0,@Now);

-- ============================================================================
-- 10) SESSION PACKAGE TEMPLATE + ASSIGNED PACKAGE + PAYMENT
-- ============================================================================
DECLARE @TplId uniqueidentifier = (SELECT TOP 1 Id FROM billing.session_package_templates
                                   WHERE GymId=@GymId AND Name='10-Session PT');
IF @TplId IS NULL
BEGIN
    SET @TplId = NEWID();
    INSERT INTO billing.session_package_templates
     (Id,GymId,Name,SessionCount,price_per_session_amount,price_per_session_currency,
      total_price_amount,total_price_currency,IsActive,CreatedAt)
    VALUES
     (@TplId,@GymId,'10-Session PT',10,300.00,'EGP',3000.00,'EGP',1,@Now);
END

IF NOT EXISTS (SELECT 1 FROM billing.session_packages WHERE MemberId=@TraineeId AND GymId=@GymId)
    INSERT INTO billing.session_packages
     (Id,GymId,TrainerId,MemberId,TemplateId,Name,TotalSessions,RemainingSessions,
      price_per_session_amount,price_per_session_currency,total_price_amount,total_price_currency,
      Status,ExpiresAt,AssignedBy,CreatedAt)
    VALUES
     (NEWID(),@GymId,@TrainerId,@TraineeId,@TplId,'10-Session PT',10,9,
      300.00,'EGP',3000.00,'EGP','Active',DATEADD(MONTH,3,@Now),@StaffId,@Now);

-- Make sure the demo trainee always resolves to Demo Trainer via the
-- TrainerAssignmentLookup. If a prior run left an exhausted/expired package
-- or a wrong TrainerId, reset it so /identity/profile populates
-- gymMemberships[].assignedTrainerId (used by the Book Session screen).
UPDATE billing.session_packages
SET    TrainerId = @TrainerId,
       Status    = 'Active',
       ExpiresAt = CASE WHEN ExpiresAt < @Now THEN DATEADD(MONTH, 3, @Now) ELSE ExpiresAt END,
       RemainingSessions = CASE WHEN RemainingSessions <= 0 THEN 9 ELSE RemainingSessions END
WHERE  MemberId = @TraineeId
  AND  GymId    = @GymId
  AND  (TrainerId <> @TrainerId OR Status <> 'Active' OR ExpiresAt < @Now OR RemainingSessions <= 0);

IF NOT EXISTS (SELECT 1 FROM billing.payments WHERE MemberId=@TraineeId AND ReceiptNumber='RCP-0001')
    INSERT INTO billing.payments
     (Id,GymId,MemberId,amount,currency,PaymentMethod,Purpose,ReferenceId,
      Status,ReceiptNumber,RecordedBy,VoidReason,PaidAt,CreatedAt)
    VALUES
     (NEWID(),@GymId,@TraineeId,3000.00,'EGP','Cash','SessionPackage',NULL,
      'Completed','RCP-0001',@StaffId,NULL,@Now,@Now);

-- ============================================================================
-- 11) TRAINER WORKOUT TEMPLATE + EXERCISE + ASSIGNMENT
-- ============================================================================
DECLARE @WorkoutId uniqueidentifier = (SELECT TOP 1 Id FROM training.workouts
                                       WHERE TrainerId=@TrainerId AND Name='Full Body Starter');
IF @WorkoutId IS NULL
BEGIN
    SET @WorkoutId = NEWID();
    INSERT INTO training.workouts
     (Id,TrainerId,GymId,Name,NameAr,Description,EstimatedDurationMinutes,
      target_muscle_groups,IsTemplate,CreatedAt,UpdatedAt)
    VALUES
     (@WorkoutId,@TrainerId,@GymId,'Full Body Starter',N'تمرين الجسم الكامل للمبتدئين',
      'Beginner full-body routine',
      45,'Chest,Back,Legs',1,@Now,@Now);
END
ELSE
    UPDATE training.workouts
    SET NameAr = N'تمرين الجسم الكامل للمبتدئين'
    WHERE Id = @WorkoutId AND (NameAr IS NULL OR NameAr = '');

DECLARE @ExerciseId uniqueidentifier = (SELECT TOP 1 Id FROM training.exercises ORDER BY Name);
IF @ExerciseId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM training.workout_exercises WHERE WorkoutId=@WorkoutId)
    INSERT INTO training.workout_exercises
     (Id,WorkoutId,ExerciseId,[Order],Sets,Reps,Weight,DurationSeconds,RestSeconds,Notes,SupersetGroupId)
    VALUES
     (NEWID(),@WorkoutId,@ExerciseId,1,3,12,20.0,NULL,60,'Warm up first',NULL);

IF NOT EXISTS (SELECT 1 FROM training.workout_assignments
               WHERE ClientId=@TraineeId AND WorkoutId=@WorkoutId)
    INSERT INTO training.workout_assignments
     (Id,TrainerId,ClientId,GymId,WorkoutId,ProgramId,AssignmentType,
      StartDate,EndDate,Status,Notes,IsRemoteMode)
    VALUES
     (NEWID(),@TrainerId,@TraineeId,@GymId,@WorkoutId,NULL,'Workout',
      @Today,DATEADD(DAY,14,@Today),'Active','Start today',0);

-- ============================================================================
-- 12) UPCOMING SESSION (tomorrow 18:00) — uses VALID Confirmed status
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM scheduling.sessions
               WHERE MemberId=@TraineeId AND TrainerId=@TrainerId
                 AND ScheduledDate=DATEADD(DAY,1,@Today))
    INSERT INTO scheduling.sessions
     (Id,GymId,TrainerId,MemberId,ScheduledDate,ScheduledTime,DurationMinutes,
      SessionType,Focus,Status,IsRecurring,RecurrenceRule,CancellationReason,
      CancelledAt,CreatedAt)
    VALUES
     (NEWID(),@GymId,@TrainerId,@TraineeId,DATEADD(DAY,1,@Today),'18:00:00',60,
      'PersonalTraining','Full Body','Confirmed',0,NULL,NULL,NULL,@Now);

-- 12b) PAST COMPLETED SESSION (yesterday 17:00) + 4 exercise logs so the
--      trainee app's session detail screen shows real per-exercise actuals
--      (sets/reps/weight + trainer notes) on first launch.
DECLARE @PastSessionId uniqueidentifier =
    (SELECT TOP 1 Id FROM scheduling.sessions
     WHERE MemberId=@TraineeId AND TrainerId=@TrainerId
       AND ScheduledDate=DATEADD(DAY,-1,@Today)
       AND Status='Completed');
IF @PastSessionId IS NULL
BEGIN
    SET @PastSessionId = NEWID();
    INSERT INTO scheduling.sessions
     (Id,GymId,TrainerId,MemberId,ScheduledDate,ScheduledTime,DurationMinutes,
      SessionType,Focus,Status,IsRecurring,RecurrenceRule,CancellationReason,
      CancelledAt,CreatedAt)
    VALUES
     (@PastSessionId,@GymId,@TrainerId,@TraineeId,DATEADD(DAY,-1,@Today),'17:00:00',60,
      'PersonalTraining','Push Day','Completed',0,NULL,NULL,NULL,@Now);
END

IF NOT EXISTS (SELECT 1 FROM scheduling.session_exercise_logs WHERE SessionId=@PastSessionId)
    INSERT INTO scheduling.session_exercise_logs
     (Id,SessionId,ExerciseId,ExerciseName,ExerciseNameAr,OrderIndex,Sets,Reps,Weight,Notes,CreatedAt,UpdatedAt)
    VALUES
     (NEWID(),@PastSessionId,NULL,'Bench Press',  N'بنش بريس',     0,4,'5',  80.0, N'Belt on top set, paused reps', @Now,NULL),
     (NEWID(),@PastSessionId,NULL,'Incline DB Press',N'ضغط دامبل مائل',1,3,'8',  22.5, N'Slow eccentric, pause at chest', @Now,NULL),
     (NEWID(),@PastSessionId,NULL,'Cable Fly',    N'كابل فلاي',     2,3,'12', 15.0, N'Squeeze 1s at peak', @Now,NULL),
     (NEWID(),@PastSessionId,NULL,'Plank',        N'بلانك',         3,3,'60s',NULL,N'Brace abs, glutes tight', @Now,NULL);

-- ============================================================================
-- 13) GROUP CLASS + ANNOUNCEMENT
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM gyms.classes WHERE GymId=@GymId AND Name='HIIT Blast')
    INSERT INTO gyms.classes
     (Id,GymId,Name,Description,TrainerId,Schedule_DayOfWeek,Schedule_StartTime,
      Schedule_DurationMinutes,Schedule_IsRecurring,Capacity,WaitlistEnabled,Status,CreatedAt)
    VALUES
     (NEWID(),@GymId,'HIIT Blast','High-intensity interval training',@TrainerId,
      'Tuesday','19:00:00',45,1,20,1,'Active',@Now);

IF NOT EXISTS (SELECT 1 FROM gyms.announcements WHERE GymId=@GymId AND Title='Welcome to Forma!')
    INSERT INTO gyms.announcements
     (Id,GymId,Title,Content,ImageUrl,Category,TargetAudience,PublishedAt,ScheduledAt,CreatedBy)
    VALUES
     (NEWID(),@GymId,'Welcome to Forma!',
      'New year promotions are live — check the front desk for details.',
      NULL,'General','Members',@Now,NULL,@OwnerId);

-- ============================================================================
-- 14) PROGRESS: body metric, streak, food log, water log for trainee
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM progress.body_metric_entries WHERE UserId=@TraineeId AND Date=@Today)
    INSERT INTO progress.body_metric_entries
     (Id,UserId,GymId,Date,Weight,WeightUnit,BodyFatPercentage,
      measurement_chest,measurement_waist,measurement_hips,
      measurement_left_arm,measurement_right_arm,
      measurement_left_thigh,measurement_right_thigh,
      measurement_left_calf,measurement_right_calf,measurement_neck,
      measurement_unit,Source,LoggedBy,Notes,CreatedAt,UpdatedAt)
    VALUES
     (NEWID(),@TraineeId,@GymId,@Today,78.5,'Kg',18.0,
      95.0,82.0,96.0,33.0,33.0,55.0,55.0,37.0,37.0,38.0,
      'Cm','SelfReported',@TraineeId,'Baseline',@Now,@Now);

IF NOT EXISTS (SELECT 1 FROM progress.streaks WHERE UserId=@TraineeId AND StreakType='DailyWorkout')
    INSERT INTO progress.streaks
     (Id,UserId,GymId,CurrentStreak,LongestStreak,StreakType,LastActivityDate,UpdatedAt)
    VALUES
     (NEWID(),@TraineeId,@GymId,3,7,'DailyWorkout',@Today,@Now);

DECLARE @FoodLogId uniqueidentifier =
    (SELECT TOP 1 Id FROM nutrition.food_logs WHERE UserId=@TraineeId AND Date=@Today);
IF @FoodLogId IS NULL
BEGIN
    SET @FoodLogId = NEWID();
    INSERT INTO nutrition.food_logs
     (Id,UserId,GymId,Date,total_calories,total_protein,total_carbs,total_fat,CreatedAt,UpdatedAt)
    VALUES (@FoodLogId,@TraineeId,@GymId,@Today,1850,135,180,60,@Now,@Now);

    INSERT INTO nutrition.meal_entries
     (Id,FoodLogId,MealType,TotalCalories,TotalProtein,TotalCarbs,TotalFat,food_items)
    VALUES
     (NEWID(),@FoodLogId,'Breakfast',450,30,45,15,
      N'[{"Name":"Oats + egg","Quantity":300,"Unit":"g","Calories":450,"Protein":30,"Carbs":45,"Fat":15}]'),
     (NEWID(),@FoodLogId,'Lunch',    700,55,70,25,
      N'[{"Name":"Chicken + rice","Quantity":400,"Unit":"g","Calories":700,"Protein":55,"Carbs":70,"Fat":25}]'),
     (NEWID(),@FoodLogId,'Dinner',   700,50,65,20,
      N'[{"Name":"Fish + salad","Quantity":350,"Unit":"g","Calories":700,"Protein":50,"Carbs":65,"Fat":20}]');
END

DECLARE @WaterLogId uniqueidentifier =
    (SELECT TOP 1 Id FROM nutrition.water_logs WHERE UserId=@TraineeId AND Date=@Today);
IF @WaterLogId IS NULL
BEGIN
    SET @WaterLogId = NEWID();
    INSERT INTO nutrition.water_logs
     (Id,UserId,GymId,Date,TotalMl,TargetMl,CreatedAt,UpdatedAt)
    VALUES (@WaterLogId,@TraineeId,@GymId,@Today,2000,3000,@Now,@Now);

    INSERT INTO nutrition.water_entries (Id,WaterLogId,AmountMl,LoggedAt)
    VALUES (NEWID(),@WaterLogId,500,DATEADD(HOUR,-6,@Now)),
           (NEWID(),@WaterLogId,500,DATEADD(HOUR,-4,@Now)),
           (NEWID(),@WaterLogId,500,DATEADD(HOUR,-2,@Now)),
           (NEWID(),@WaterLogId,500,@Now);
END

-- ============================================================================
-- 15) TRAINER/MEMBER/LEADERBOARD SEEDING — APPLY TO EVERY GYM THE TRAINER
--     IS ACTIVELY A MEMBER OF
--     The repo has historically accumulated multiple demo gyms (e.g. "Demo
--     Fitness Club" from DemoGymSeeder and pre-existing "Forma Fitness Cairo").
--     trainer@demo.local can belong to both. Rather than pick one and leave the
--     other empty, we iterate over every active Trainer membership the demo
--     trainer has and seed availability, member/trainer ID collections, and
--     current-month leaderboards for each gym.
-- ============================================================================
DECLARE @CurMonth int = MONTH(@Today);
DECLARE @CurYear  int = YEAR(@Today);

DECLARE @TrainerGyms TABLE (GymId uniqueidentifier);
INSERT INTO @TrainerGyms (GymId)
SELECT DISTINCT m.gym_id
FROM [identity].user_gym_memberships m
WHERE m.UserId = @TrainerId AND m.is_active = 1;

-- Make sure any trainer_rate entries exist for every such gym (the existing
-- section 9 only inserted for @GymId resolved via owner@demo.local).
INSERT INTO gyms.trainer_rates (Id, GymId, TrainerId, RatePerSession, Currency, EffectiveFrom, SetBy)
SELECT NEWID(), tg.GymId, @TrainerId, 300.00, 'EGP', @Now, @OwnerId
FROM @TrainerGyms tg
WHERE NOT EXISTS (
    SELECT 1 FROM gyms.trainer_rates tr
    WHERE tr.GymId = tg.GymId AND tr.TrainerId = @TrainerId);

-- 15.1) Trainer availability + Mon–Fri 10:00–18:00 slots per gym
DECLARE @iterGymId uniqueidentifier;
DECLARE @AvailId uniqueidentifier;

DECLARE gym_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT GymId FROM @TrainerGyms;
OPEN gym_cursor;
FETCH NEXT FROM gym_cursor INTO @iterGymId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @AvailId = Id FROM scheduling.trainer_availabilities
    WHERE TrainerId = @TrainerId AND GymId = @iterGymId;

    IF @AvailId IS NULL
    BEGIN
        SET @AvailId = NEWID();
        INSERT INTO scheduling.trainer_availabilities
         (Id, TrainerId, GymId, SessionDurationMinutes, BufferMinutes, UpdatedAt)
        VALUES (@AvailId, @TrainerId, @iterGymId, 60, 15, @Now);

        INSERT INTO scheduling.availability_slots
         (Id, TrainerAvailabilityId, DayOfWeek, StartTime, EndTime, IsActive)
        VALUES
         (NEWID(), @AvailId, 1, '10:00:00', '18:00:00', 1),
         (NEWID(), @AvailId, 2, '10:00:00', '18:00:00', 1),
         (NEWID(), @AvailId, 3, '10:00:00', '18:00:00', 1),
         (NEWID(), @AvailId, 4, '10:00:00', '18:00:00', 1),
         (NEWID(), @AvailId, 5, '10:00:00', '18:00:00', 1);
    END

    SET @AvailId = NULL;
    FETCH NEXT FROM gym_cursor INTO @iterGymId;
END
CLOSE gym_cursor;
DEALLOCATE gym_cursor;

-- 15.2) Gym.MemberIds / Gym.TrainerIds JSON collections — the Gyms module
--       tracks these as EF Core PrimitiveCollection JSON arrays on the gym
--       row. Without them, GET /gyms/{id}/members and /trainers return [].
--       Populate for every gym the trainer belongs to, using the trainees
--       that share that gym's memberships.
DECLARE @g uniqueidentifier;
DECLARE gym_ids CURSOR LOCAL FAST_FORWARD FOR
    SELECT GymId FROM @TrainerGyms;
OPEN gym_ids;
FETCH NEXT FROM gym_ids INTO @g;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @mJson nvarchar(max) = (
        SELECT '[' + STUFF((
            SELECT ',"' + CAST(m.UserId AS nvarchar(36)) + '"'
            FROM [identity].user_gym_memberships m
            WHERE m.gym_id = @g AND m.is_active = 1 AND m.role = 'Trainee'
            FOR XML PATH('')), 1, 1, '') + ']');

    DECLARE @tJson nvarchar(max) = (
        SELECT '[' + STUFF((
            SELECT ',"' + CAST(m.UserId AS nvarchar(36)) + '"'
            FROM [identity].user_gym_memberships m
            WHERE m.gym_id = @g AND m.is_active = 1 AND m.role = 'Trainer'
            FOR XML PATH('')), 1, 1, '') + ']');

    UPDATE gyms.gyms
    SET MemberIds  = ISNULL(@mJson, '[]'),
        TrainerIds = ISNULL(@tJson, '[]'),
        UpdatedAt  = @Now
    WHERE Id = @g;

    SET @mJson = NULL; SET @tJson = NULL;
    FETCH NEXT FROM gym_ids INTO @g;
END
CLOSE gym_ids; DEALLOCATE gym_ids;

-- 15.2b) gyms.gym_members — per-(gym,member) directory that stores the
--        human-facing MemberNumber + Section. Reception-desk flows like
--        POST /gyms/{id}/check-in (memberNumber path) look rows up here.
--        Copy from identity.user_gym_memberships where member_number is
--        set; auto-assign sequential 4-digit numbers for trainees that
--        have no number yet so the demo always has something to search by.
DECLARE @GymIdScoped uniqueidentifier;
DECLARE gym_members_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT GymId FROM @TrainerGyms;
OPEN gym_members_cursor;
FETCH NEXT FROM gym_members_cursor INTO @GymIdScoped;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Copy numbers that already exist in identity memberships.
    INSERT INTO gyms.gym_members (Id, GymId, MemberId, MemberNumber, Section, JoinedAt)
    SELECT NEWID(), m.gym_id, m.UserId, m.member_number, m.section, m.joined_at
    FROM   [identity].user_gym_memberships m
    WHERE  m.gym_id = @GymIdScoped
      AND  m.is_active = 1
      AND  m.role = 'Trainee'
      AND  m.member_number IS NOT NULL
      AND  NOT EXISTS (
           SELECT 1 FROM gyms.gym_members gm
           WHERE  gm.GymId = m.gym_id AND gm.MemberId = m.UserId);

    -- Assign sequential numbers for trainees still missing one.
    DECLARE @next int = ISNULL((
        SELECT MAX(TRY_CAST(MemberNumber AS int))
        FROM   gyms.gym_members WHERE GymId = @GymIdScoped), 0) + 1;

    DECLARE @uid uniqueidentifier, @section nvarchar(60), @joined datetime2;
    DECLARE unnumbered CURSOR LOCAL FAST_FORWARD FOR
        SELECT m.UserId, m.section, m.joined_at
        FROM   [identity].user_gym_memberships m
        WHERE  m.gym_id = @GymIdScoped
          AND  m.is_active = 1
          AND  m.role = 'Trainee'
          AND  NOT EXISTS (
               SELECT 1 FROM gyms.gym_members gm
               WHERE  gm.GymId = m.gym_id AND gm.MemberId = m.UserId);
    OPEN unnumbered;
    FETCH NEXT FROM unnumbered INTO @uid, @section, @joined;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO gyms.gym_members (Id, GymId, MemberId, MemberNumber, Section, JoinedAt)
        VALUES (NEWID(), @GymIdScoped, @uid, RIGHT('0000' + CAST(@next AS varchar(10)), 4), @section, @joined);
        SET @next = @next + 1;
        FETCH NEXT FROM unnumbered INTO @uid, @section, @joined;
    END
    CLOSE unnumbered; DEALLOCATE unnumbered;

    FETCH NEXT FROM gym_members_cursor INTO @GymIdScoped;
END
CLOSE gym_members_cursor; DEALLOCATE gym_members_cursor;

-- 15.3) Member leaderboards (Attendance, WeightLoss, Consistency) for current
--       month — one per gym. workout-streak / workout_streak is NOT a valid
--       LeaderboardType member; clients should only request the three above.
DECLARE @lbTypes TABLE (LeaderboardType nvarchar(30));
INSERT INTO @lbTypes VALUES ('Attendance'),('WeightLoss'),('Consistency');

INSERT INTO gamification.member_leaderboards
 (Id, GymId, period_month, period_year, LeaderboardType, RecalculatedAt, CreatedAt)
SELECT NEWID(), tg.GymId, @CurMonth, @CurYear, t.LeaderboardType, @Now, @Now
FROM @TrainerGyms tg
CROSS JOIN @lbTypes t
WHERE NOT EXISTS (
    SELECT 1 FROM gamification.member_leaderboards ml
    WHERE ml.GymId = tg.GymId
      AND ml.period_month = @CurMonth
      AND ml.period_year = @CurYear
      AND ml.LeaderboardType = t.LeaderboardType);

-- Leaderboard entries for all demo trainees so the UI shows a ranked list.
-- trainee2 leads (rank 1), trainee3 second (rank 2), canonical trainee third (rank 3).
DECLARE @LeaderboardSeeds TABLE (MemberId uniqueidentifier, Rank int, Score int, Delta int);
INSERT INTO @LeaderboardSeeds VALUES
    (@Trainee2Id, 1, 42, 5),
    (@Trainee3Id, 2, 28, -2),
    (@TraineeId,  3, 17, 3);

INSERT INTO gamification.leaderboard_entries
 (Id, LeaderboardId, MemberId, Rank, Score, Delta, IsOptedIn)
SELECT NEWID(), ml.Id, s.MemberId, s.Rank, s.Score, s.Delta, 1
FROM gamification.member_leaderboards ml
JOIN @TrainerGyms tg ON tg.GymId = ml.GymId
CROSS JOIN @LeaderboardSeeds s
WHERE ml.period_month = @CurMonth
  AND ml.period_year = @CurYear
  AND NOT EXISTS (
      SELECT 1 FROM gamification.leaderboard_entries le
      WHERE le.LeaderboardId = ml.Id AND le.MemberId = s.MemberId);

-- 15.4) Trainer public profile for every gym (sec 9 only covered @GymId).
-- DisplayRating = 0 so the UI shows the "No ratings yet" empty state until
-- real client ratings come in (see section 9 comment).
INSERT INTO gamification.trainer_public_profiles
 (Id, TrainerId, GymId, DisplayRating, CoachOfTheMonthCount, UpdatedAt)
SELECT NEWID(), @TrainerId, tg.GymId, 0, 0, @Now
FROM @TrainerGyms tg
WHERE NOT EXISTS (
    SELECT 1 FROM gamification.trainer_public_profiles p
    WHERE p.TrainerId = @TrainerId AND p.GymId = tg.GymId);

-- 15.5) Trainer monthly score for demo trainer — ensures Coach Rankings tab
--       always has data even when section 16a hasn't run.
DECLARE @CurMonth15 int = MONTH(@Now);
DECLARE @CurYear15  int = YEAR(@Now);
INSERT INTO gamification.trainer_monthly_scores
 (Id, GymId, TrainerId, Month, Year, AggregatedScore, TotalRatings,
  UniqueClients, SessionCount, NormalizedScore, IsCoachOfTheMonth, CalculatedAt)
SELECT NEWID(), tg.GymId, @TrainerId, @CurMonth15, @CurYear15,
       320.00, 8, 5, 16, 0.7500, 0, @Now
FROM @TrainerGyms tg
WHERE NOT EXISTS (
    SELECT 1 FROM gamification.trainer_monthly_scores s
    WHERE s.GymId = tg.GymId AND s.TrainerId = @TrainerId
      AND s.Month = @CurMonth15 AND s.Year = @CurYear15);

-- ============================================================================
-- 16a) ADDITIONAL DEMO DATA FOR E2E TESTS
--      Adds: 2 trainers (1 active, 1 pending), 1 extra class, 1 announcement,
--      2 extra members (Ahmed + Yasser), 1 Manager staff member.
-- ============================================================================

-- Extra trainer 1: Captain Mohamed Ali (active / approved)
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='trainer.ali@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'trainer.ali@demo.local','+201100000021',@Hash,'Captain Mohamed','Ali',
            NULL, NULL, 'Male', 'Trainer', 'Active','Arabic', @Now, @Now);

DECLARE @TrainerAliId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainer.ali@demo.local');

IF NOT EXISTS (SELECT 1 FROM [identity].user_gym_memberships WHERE UserId=@TrainerAliId AND gym_id=@GymId)
    INSERT INTO [identity].user_gym_memberships (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES (@GymId, 'Trainer', @Now, 1, NULL, NULL, @TrainerAliId);

IF NOT EXISTS (SELECT 1 FROM gyms.trainer_rates WHERE GymId=@GymId AND TrainerId=@TrainerAliId)
    INSERT INTO gyms.trainer_rates (Id, GymId, TrainerId, RatePerSession, Currency, EffectiveFrom, SetBy)
    VALUES (NEWID(), @GymId, @TrainerAliId, 350.00, 'EGP', @Now, @OwnerId);

-- Extra trainer 2: Captain Amr Soliman (pending approval)
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='trainer.soliman@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'trainer.soliman@demo.local','+201100000022',@Hash,'Captain Amr','Soliman',
            NULL, NULL, 'Male', 'Trainer', 'Active','Arabic', @Now, @Now);

DECLARE @TrainerSolimanId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='trainer.soliman@demo.local');

-- Pending trainer only gets a GymTrainerRequest (NOT added to user_gym_memberships as Trainer)
-- so the section 15.2 cursor doesn't add him to TrainerIds JSON.
-- The backend includes him via pendingIds from gym_trainer_requests.
IF NOT EXISTS (SELECT 1 FROM [identity].user_gym_memberships WHERE UserId=@TrainerSolimanId AND gym_id=@GymId)
    INSERT INTO [identity].user_gym_memberships (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES (@GymId, 'Trainee', @Now, 0, NULL, NULL, @TrainerSolimanId);

IF NOT EXISTS (SELECT 1 FROM gyms.gym_trainer_requests WHERE GymId=@GymId AND TrainerId=@TrainerSolimanId)
    INSERT INTO gyms.gym_trainer_requests (Id, GymId, TrainerId, Status, RequestedAt, DecidedAt)
    VALUES (NEWID(), @GymId, @TrainerSolimanId, 'Pending', @Now, NULL);

-- Gamification profile for Captain Mohamed Ali
IF NOT EXISTS (SELECT 1 FROM gamification.trainer_public_profiles WHERE TrainerId=@TrainerAliId AND GymId=@GymId)
    INSERT INTO gamification.trainer_public_profiles (Id, TrainerId, GymId, DisplayRating, CoachOfTheMonthCount, UpdatedAt)
    VALUES (NEWID(), @TrainerAliId, @GymId, 0, 0, @Now);

-- Morning Yoga class
IF NOT EXISTS (SELECT 1 FROM gyms.classes WHERE GymId=@GymId AND Name='Morning Yoga')
    INSERT INTO gyms.classes
     (Id, GymId, Name, Description, TrainerId, Schedule_DayOfWeek, Schedule_StartTime,
      Schedule_DurationMinutes, Schedule_IsRecurring, Capacity, WaitlistEnabled, Status, CreatedAt)
    VALUES
     (NEWID(), @GymId, 'Morning Yoga', 'Relaxing morning yoga for all levels', @TrainerAliId,
      'Monday', '08:00:00', 60, 1, 15, 0, 'Active', @Now);

-- Ramadan Hours announcement
IF NOT EXISTS (SELECT 1 FROM gyms.announcements WHERE GymId=@GymId AND Title='Ramadan Hours')
    INSERT INTO gyms.announcements
     (Id, GymId, Title, Content, ImageUrl, Category, TargetAudience, PublishedAt, ScheduledAt, CreatedBy)
    VALUES
     (NEWID(), @GymId, 'Ramadan Hours',
      N'During Ramadan our gym hours are adjusted. Morning sessions: 08:00–11:00. Evening sessions: 21:00–01:00.',
      NULL, 'General', 'All', @Now, NULL, @OwnerId);

-- Member: Ahmed Khaled (for check-in filter test)
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='ahmed@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'ahmed@demo.local','+201200000031',@Hash,'Ahmed','Khaled',
            NULL, NULL, 'Male', 'Trainee', 'Active','Arabic', @Now, @Now);

DECLARE @AhmedId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='ahmed@demo.local');

IF NOT EXISTS (SELECT 1 FROM [identity].user_gym_memberships WHERE UserId=@AhmedId AND gym_id=@GymId)
    INSERT INTO [identity].user_gym_memberships (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES (@GymId, 'Trainee', @Now, 1, 'Mens', 'M-0031', @AhmedId);

-- Member: Yasser Abdallah (leaderboard rank 1)
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='yasser@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'yasser@demo.local','+201200000032',@Hash,'Yasser','Abdallah',
            NULL, NULL, 'Male', 'Trainee', 'Active','Arabic', @Now, @Now);

DECLARE @YasserId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='yasser@demo.local');

IF NOT EXISTS (SELECT 1 FROM [identity].user_gym_memberships WHERE UserId=@YasserId AND gym_id=@GymId)
    INSERT INTO [identity].user_gym_memberships (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES (@GymId, 'Trainee', @Now, 1, 'Mens', 'M-0032', @YasserId);

-- PDPL consents for new members and trainers
INSERT INTO [identity].user_consents
 (Id, user_id, consent_type, is_granted, granted_at, withdrawn_at, consent_text, ip_address)
SELECT NEWID(), u.Id, ct.ConsentType, 1, @Now, NULL, 'Demo seed consent', '127.0.0.1'
FROM [identity].users u
CROSS JOIN (VALUES ('DataProcessing'),('Marketing')) ct(ConsentType)
WHERE u.email IN ('trainer.ali@demo.local','trainer.soliman@demo.local',
                  'ahmed@demo.local','yasser@demo.local')
  AND NOT EXISTS (
      SELECT 1 FROM [identity].user_consents c
      WHERE c.user_id = u.Id AND c.consent_type = ct.ConsentType);

-- Staff: Mostafa Kamel (Manager)
IF NOT EXISTS (SELECT 1 FROM [identity].users WHERE email='mostafa@demo.local')
    INSERT INTO [identity].users (Id, email, phone_number, password_hash, first_name, last_name,
        profile_photo_url, date_of_birth, gender, role, status, language, created_at, updated_at)
    VALUES (NEWID(),'mostafa@demo.local','+201300000041',@Hash,'Mostafa','Kamel',
            NULL, NULL, 'Male', 'GymStaff', 'Active','Arabic', @Now, @Now);

DECLARE @MostafaId uniqueidentifier = (SELECT Id FROM [identity].users WHERE email='mostafa@demo.local');

IF NOT EXISTS (SELECT 1 FROM [identity].user_gym_memberships WHERE UserId=@MostafaId AND gym_id=@GymId)
    INSERT INTO [identity].user_gym_memberships (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES (@GymId, 'GymStaff', @Now, 1, NULL, NULL, @MostafaId);

IF NOT EXISTS (SELECT 1 FROM gyms.staff_roles WHERE GymId=@GymId AND UserId=@MostafaId)
    INSERT INTO gyms.staff_roles (Id, GymId, UserId, Role)
    VALUES (NEWID(), @GymId, @MostafaId, 'Manager');

-- PDPL consent for Mostafa
INSERT INTO [identity].user_consents
 (Id, user_id, consent_type, is_granted, granted_at, withdrawn_at, consent_text, ip_address)
SELECT NEWID(), @MostafaId, ct.ConsentType, 1, @Now, NULL, 'Demo seed consent', '127.0.0.1'
FROM (VALUES ('DataProcessing'),('Marketing')) ct(ConsentType)
WHERE NOT EXISTS (
    SELECT 1 FROM [identity].user_consents c
    WHERE c.user_id = @MostafaId AND c.consent_type = ct.ConsentType);

-- Trainer monthly score for Captain Mohamed Ali (Coach Rankings)
IF NOT EXISTS (SELECT 1 FROM gamification.trainer_monthly_scores
               WHERE GymId=@GymId AND TrainerId=@TrainerAliId
                 AND Month=@CurMonth AND Year=@CurYear)
    INSERT INTO gamification.trainer_monthly_scores
     (Id, GymId, TrainerId, Month, Year, AggregatedScore, TotalRatings,
      UniqueClients, SessionCount, NormalizedScore, IsCoachOfTheMonth, CalculatedAt)
    VALUES
     (NEWID(), @GymId, @TrainerAliId, @CurMonth, @CurYear, 480.00, 12,
      8, 24, 0.9200, 1, @Now);

-- Also seed trainer monthly score for original demo trainer (so leaderboard isn't empty)
IF NOT EXISTS (SELECT 1 FROM gamification.trainer_monthly_scores
               WHERE GymId=@GymId AND TrainerId=@TrainerId
                 AND Month=@CurMonth AND Year=@CurYear)
    INSERT INTO gamification.trainer_monthly_scores
     (Id, GymId, TrainerId, Month, Year, AggregatedScore, TotalRatings,
      UniqueClients, SessionCount, NormalizedScore, IsCoachOfTheMonth, CalculatedAt)
    VALUES
     (NEWID(), @GymId, @TrainerId, @CurMonth, @CurYear, 320.00, 8,
      5, 16, 0.7500, 0, @Now);

-- Rebuild TrainerIds and MemberIds JSON to include new Trainer
UPDATE gyms.gyms
SET TrainerIds = (
    SELECT '[' + STUFF((
        SELECT ',"' + CAST(m.UserId AS nvarchar(36)) + '"'
        FROM [identity].user_gym_memberships m
        WHERE m.gym_id = @GymId AND m.is_active = 1 AND m.role = 'Trainer'
        FOR XML PATH('')), 1, 1, '') + ']'),
    MemberIds = (
    SELECT '[' + STUFF((
        SELECT ',"' + CAST(m.UserId AS nvarchar(36)) + '"'
        FROM [identity].user_gym_memberships m
        WHERE m.gym_id = @GymId AND m.is_active = 1 AND m.role = 'Trainee'
        FOR XML PATH('')), 1, 1, '') + ']'),
    UpdatedAt = @Now
WHERE Id = @GymId;

-- Leaderboard entry for Yasser Abdallah (rank 1) replacing trainee2's rank
-- Only update existing entries; if no leaderboard exists yet section 15 handles creation.
UPDATE gamification.leaderboard_entries
SET MemberId = @YasserId, Rank = 1, Score = 95, Delta = 10
WHERE LeaderboardId IN (
    SELECT Id FROM gamification.member_leaderboards
    WHERE GymId = @GymId AND period_month = @CurMonth AND period_year = @CurYear)
  AND MemberId = @Trainee2Id;

-- gym_members rows for Ahmed and Yasser
INSERT INTO gyms.gym_members (Id, GymId, MemberId, MemberNumber, Section, JoinedAt)
SELECT NEWID(), @GymId, u.Id, m.member_number, m.section, m.joined_at
FROM [identity].user_gym_memberships m
JOIN [identity].users u ON u.Id = m.UserId
WHERE m.gym_id = @GymId AND u.email IN ('ahmed@demo.local','yasser@demo.local')
  AND m.member_number IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM gyms.gym_members gm WHERE gm.GymId = @GymId AND gm.MemberId = u.Id);

-- ============================================================================
-- 16) SUMMARY
-- ============================================================================
PRINT '=== GymApp master seed complete — all demo users use password: 123456 ===';

SELECT email, role, status FROM [identity].users
WHERE email IN ('admin@demo.local','owner@demo.local','staff@demo.local',
                'trainer@demo.local','trainee@demo.local',
                'trainee2@demo.local','trainee3@demo.local')
ORDER BY role, email;

SELECT 'features'            AS entity, COUNT(*) AS cnt FROM billing.features
UNION ALL SELECT 'plan_features',        COUNT(*) FROM billing.plan_features WHERE PlanId = @PlanId
UNION ALL SELECT 'gym_subscription',     COUNT(*) FROM billing.gym_subscriptions WHERE GymId = @GymId
UNION ALL SELECT 'memberships',          COUNT(*) FROM [identity].user_gym_memberships WHERE gym_id = @GymId
UNION ALL SELECT 'session_packages',     COUNT(*) FROM billing.session_packages WHERE GymId = @GymId
UNION ALL SELECT 'payments',             COUNT(*) FROM billing.payments WHERE GymId = @GymId
UNION ALL SELECT 'sessions',             COUNT(*) FROM scheduling.sessions WHERE GymId = @GymId
UNION ALL SELECT 'classes',              COUNT(*) FROM gyms.classes WHERE GymId = @GymId
UNION ALL SELECT 'announcements',        COUNT(*) FROM gyms.announcements WHERE GymId = @GymId
UNION ALL SELECT 'workouts',             COUNT(*) FROM training.workouts WHERE GymId = @GymId
UNION ALL SELECT 'workout_assignments',  COUNT(*) FROM training.workout_assignments WHERE GymId = @GymId
UNION ALL SELECT 'body_metrics',         COUNT(*) FROM progress.body_metric_entries WHERE GymId = @GymId
UNION ALL SELECT 'food_logs',            COUNT(*) FROM nutrition.food_logs WHERE GymId = @GymId
UNION ALL SELECT 'water_logs',           COUNT(*) FROM nutrition.water_logs WHERE GymId = @GymId
UNION ALL SELECT 'trainer_availabilities', COUNT(*) FROM scheduling.trainer_availabilities WHERE GymId = @GymId
UNION ALL SELECT 'availability_slots',   COUNT(*) FROM scheduling.availability_slots s JOIN scheduling.trainer_availabilities a ON s.TrainerAvailabilityId = a.Id WHERE a.GymId = @GymId
UNION ALL SELECT 'member_leaderboards',  COUNT(*) FROM gamification.member_leaderboards WHERE GymId = @GymId
UNION ALL SELECT 'leaderboard_entries',  COUNT(*) FROM gamification.leaderboard_entries le JOIN gamification.member_leaderboards ml ON le.LeaderboardId = ml.Id WHERE ml.GymId = @GymId;

EndOfSeed:
PRINT 'seed.sql complete.';
