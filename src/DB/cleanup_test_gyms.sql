-- =============================================================================
-- cleanup_test_gyms.sql
-- Removes leftover Test/E2E/BUG/Approve gyms and all dependent rows.
-- Excludes real demo gyms ('First Gym', 'FirstGym', 'Second Gym', 'Forma').
-- Idempotent: safe to re-run.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @TargetIds TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);

INSERT INTO @TargetIds (Id)
SELECT Id FROM g1_gyms.gyms
WHERE Name LIKE 'Test Gym BUG%'
   OR Name LIKE 'BUG3%'
   OR Name LIKE 'E2E Gym%'
   OR Name LIKE 'Approve UI%'
   OR Name LIKE 'Approve E2E%';

DECLARE @TargetCount INT = (SELECT COUNT(*) FROM @TargetIds);
PRINT CONCAT('Target gyms: ', @TargetCount);

BEGIN TRAN;

-- g1_gyms child tables
DELETE FROM g1_gyms.gym_operating_hours       WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.gym_seasonal_day_schedules WHERE GymSeasonalHoursId IN (SELECT Id FROM g1_gyms.gym_seasonal_hours WHERE GymId IN (SELECT Id FROM @TargetIds));
DELETE FROM g1_gyms.gym_seasonal_hours        WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.gym_session_policies      WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.gym_sections              WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.gym_trainer_requests      WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.gym_members               WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.announcements             WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.check_ins                 WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.classes                   WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.data_import_jobs          WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.member_notes              WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.member_onboardings        WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.staff_role_permissions    WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.staff_roles               WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gyms.trainer_rates             WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_billing
DELETE FROM g1_billing.gym_feature_overrides     WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.gym_storage_usage         WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.gym_subscriptions         WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.membership_plans          WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.payments                  WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.plan_change_log           WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.session_package_templates WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.session_packages          WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_billing.trainer_earnings_records  WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_communication
DELETE FROM g1_communication.notifications            WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_communication.notification_preferences WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_communication.conversations            WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_gamification
DELETE FROM g1_gamification.leaderboard_opt_outs     WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gamification.member_leaderboards      WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gamification.trainer_monthly_scores   WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gamification.trainer_public_profiles  WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_gamification.trainer_ratings          WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_nutrition
DELETE FROM g1_nutrition.food_logs   WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_nutrition.meal_plans  WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_nutrition.water_logs  WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_progress
DELETE FROM g1_progress.body_metric_entries WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_progress.progress_photos     WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_progress.streaks             WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_scheduling
DELETE FROM g1_scheduling.sessions               WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_scheduling.trainer_availabilities WHERE GymId IN (SELECT Id FROM @TargetIds);

-- g1_training
DELETE FROM g1_training.workout_logs           WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_training.workout_assignments    WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_training.workouts               WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_training.programs               WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_training.exercises              WHERE GymId IN (SELECT Id FROM @TargetIds);
DELETE FROM g1_training.remote_workout_configs WHERE GymId IN (SELECT Id FROM @TargetIds);

-- Identity memberships keyed by gym_id (snake_case)
DELETE FROM [identity].user_gym_memberships WHERE gym_id IN (SELECT Id FROM @TargetIds);

-- Cross-tenant registry
DELETE FROM gyms_registry.gyms WHERE GymId IN (SELECT Id FROM @TargetIds);

-- Finally the gym aggregates themselves
DELETE FROM g1_gyms.gyms WHERE Id IN (SELECT Id FROM @TargetIds);

COMMIT;

PRINT 'Cleanup committed.';
SELECT COUNT(*) AS RemainingTestGyms FROM g1_gyms.gyms
WHERE Name LIKE 'Test Gym BUG%' OR Name LIKE 'BUG3%' OR Name LIKE 'E2E Gym%' OR Name LIKE 'Approve UI%' OR Name LIKE 'Approve E2E%';
