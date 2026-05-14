-- =============================================================================
-- cleanup_demo_data.sql
-- Production-prep wipe of demo gym + demo accounts + placeholder trainer accounts.
-- Run AFTER deploying the code changes that disable seeders by default.
--
-- Removes:
--   * "FirstGym" (DEMO01) and ALL dependent rows across g1_* schemas
--   * Demo users: admin@/owner@/staff@/trainer@/trainee@demo.local + numbered demo trainees
--   * Placeholder trainer accounts (email LIKE '%@placeholder.gymapp.local')
--   * Identity gym memberships and cross-tenant registry entries for the above
--
-- Idempotent: re-running on already-clean DB is a no-op.
-- Wrap in a transaction; rolls back on any error.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;

-- ---- Resolve target gym(s) ----
DECLARE @TargetGyms TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);

INSERT INTO @TargetGyms (Id)
SELECT Id
FROM g1_gyms.gyms
WHERE Name IN (N'FirstGym', N'First Gym')
   OR GymCode = 'DEMO01';

DECLARE @GymCount INT = (SELECT COUNT(*) FROM @TargetGyms);
PRINT CONCAT('Demo gyms targeted: ', @GymCount);

-- ---- Resolve demo + placeholder users ----
DECLARE @TargetUsers TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);

INSERT INTO @TargetUsers (Id)
SELECT Id FROM [identity].users
WHERE email LIKE '%@demo.local'
   OR email LIKE '%@placeholder.gymapp.local'
   OR email LIKE '%@trainer.local';   -- old fake-email pattern (pre-cleanup builds)

DECLARE @UserCount INT = (SELECT COUNT(*) FROM @TargetUsers);
PRINT CONCAT('Demo / placeholder users targeted: ', @UserCount);

BEGIN TRAN;

-- =========================================================================
-- 1) Per-gym data (g1_* schemas) for the demo gym
-- =========================================================================

-- g1_gyms child tables
DELETE FROM g1_gyms.gym_operating_hours       WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gym_seasonal_day_schedules
    WHERE GymSeasonalHoursId IN (SELECT Id FROM g1_gyms.gym_seasonal_hours WHERE GymId IN (SELECT Id FROM @TargetGyms));
DELETE FROM g1_gyms.gym_seasonal_hours        WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gym_session_policies      WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gym_sections              WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gym_trainer_requests      WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gym_members               WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.announcements             WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.check_ins                 WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.classes                   WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.data_import_jobs          WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.member_notes              WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.member_onboardings        WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.staff_role_permissions    WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.staff_roles               WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.trainer_rates             WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_billing
DELETE FROM g1_billing.gym_feature_overrides     WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.gym_storage_usage         WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.gym_subscriptions         WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.membership_plans          WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.payments                  WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.plan_change_log           WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.session_package_templates WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.session_packages          WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_billing.trainer_earnings_records  WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_communication
DELETE FROM g1_communication.notifications            WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_communication.notification_preferences WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_communication.conversations            WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_gamification
DELETE FROM g1_gamification.leaderboard_opt_outs     WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gamification.member_leaderboards      WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gamification.trainer_monthly_scores   WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gamification.trainer_public_profiles  WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gamification.trainer_ratings          WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_nutrition
DELETE FROM g1_nutrition.food_logs   WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_nutrition.meal_plans  WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_nutrition.water_logs  WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_progress
DELETE FROM g1_progress.body_metric_entries WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_progress.progress_photos     WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_progress.streaks             WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_scheduling
DELETE FROM g1_scheduling.sessions               WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_scheduling.trainer_availabilities WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- g1_training
DELETE FROM g1_training.workout_logs           WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_training.workout_assignments    WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_training.workouts               WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_training.programs               WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_training.exercises              WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_training.remote_workout_configs WHERE GymId IN (SELECT Id FROM @TargetGyms);

-- =========================================================================
-- 2) Identity-side cleanup: gym memberships + the demo / placeholder users
-- =========================================================================

-- Memberships pointing at the demo gym (regardless of user)
DELETE FROM [identity].user_gym_memberships
WHERE gym_id IN (SELECT Id FROM @TargetGyms);

-- Memberships for demo / placeholder users at any gym
DELETE FROM [identity].user_gym_memberships
WHERE user_id IN (SELECT Id FROM @TargetUsers);

-- Refresh tokens / OTPs / consent for the demo / placeholder users
IF OBJECT_ID('[identity].refresh_tokens', 'U') IS NOT NULL
    DELETE FROM [identity].refresh_tokens WHERE user_id IN (SELECT Id FROM @TargetUsers);
IF OBJECT_ID('[identity].otps', 'U') IS NOT NULL
    DELETE FROM [identity].otps WHERE user_id IN (SELECT Id FROM @TargetUsers);
IF OBJECT_ID('[identity].consents', 'U') IS NOT NULL
    DELETE FROM [identity].consents WHERE user_id IN (SELECT Id FROM @TargetUsers);

-- The user rows themselves
DELETE FROM [identity].users WHERE Id IN (SELECT Id FROM @TargetUsers);

-- =========================================================================
-- 3) Cross-tenant registry + the gym aggregate
-- =========================================================================
DELETE FROM gyms_registry.gyms WHERE GymId IN (SELECT Id FROM @TargetGyms);
DELETE FROM g1_gyms.gyms       WHERE Id    IN (SELECT Id FROM @TargetGyms);

COMMIT;

PRINT 'Demo + placeholder data wiped.';

-- ---- Verification ----
SELECT COUNT(*) AS RemainingDemoGyms       FROM g1_gyms.gyms       WHERE Name IN (N'FirstGym', N'First Gym') OR GymCode = 'DEMO01';
SELECT COUNT(*) AS RemainingDemoUsers      FROM [identity].users   WHERE email LIKE '%@demo.local';
SELECT COUNT(*) AS RemainingPlaceholders   FROM [identity].users   WHERE email LIKE '%@placeholder.gymapp.local' OR email LIKE '%@trainer.local';
SELECT COUNT(*) AS RemainingDemoMemberships FROM [identity].user_gym_memberships WHERE gym_id IN (SELECT Id FROM @TargetGyms);
