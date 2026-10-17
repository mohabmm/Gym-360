namespace GymApp.Common.Infrastructure.Tenancy.Provisioning;

/// <summary>
/// T-SQL that copies the billing reference catalog (plans, features, plan-feature
/// matrix, storage limits) into a gym's <c>g{N}_billing</c> schema and ensures the
/// gym has a usable subscription. Parameters: <c>@gymId</c>, <c>@ownerUserId</c>.
/// </summary>
internal static class BillingHydrationSql
{
    private const string PrefixToken = "{prefix}";

    private const string Template = @"
SET XACT_ABORT ON;
BEGIN TRAN;
-- Plans: insert missing, then sync editable fields so catalog edits in
-- billing.subscription_plans propagate to per-gym schemas. Code/Id are stable
-- keys (FKs depend on Id), so they're never overwritten.
INSERT INTO [{prefix}_billing].subscription_plans
  (Id, Code, Name, Description, MonthlyPriceEgp, AnnualPriceEgp, MemberLimit, TrainerLimit, StaffLimit, IsActive, IsPublic, IsTimeLimited, AvailableFrom, AvailableUntil, SortOrder, CreatedAt, UpdatedAt)
SELECT Id, Code, Name, Description, MonthlyPriceEgp, AnnualPriceEgp, MemberLimit, TrainerLimit, StaffLimit, IsActive, IsPublic, IsTimeLimited, AvailableFrom, AvailableUntil, SortOrder, CreatedAt, UpdatedAt
FROM billing.subscription_plans s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].subscription_plans g WHERE g.Id = s.Id);

UPDATE g
SET    g.Name = s.Name, g.Description = s.Description,
       g.MonthlyPriceEgp = s.MonthlyPriceEgp, g.AnnualPriceEgp = s.AnnualPriceEgp,
       g.MemberLimit = s.MemberLimit, g.TrainerLimit = s.TrainerLimit, g.StaffLimit = s.StaffLimit,
       g.IsActive = s.IsActive, g.IsPublic = s.IsPublic, g.IsTimeLimited = s.IsTimeLimited,
       g.AvailableFrom = s.AvailableFrom, g.AvailableUntil = s.AvailableUntil,
       g.SortOrder = s.SortOrder, g.UpdatedAt = SYSUTCDATETIME()
FROM   [{prefix}_billing].subscription_plans g
JOIN   billing.subscription_plans s ON s.Id = g.Id;

-- Features: insert missing, sync metadata.
INSERT INTO [{prefix}_billing].features (Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt)
SELECT Id, Code, Name, Description, Category, IsActive, SortOrder, CreatedAt
FROM billing.features s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].features g WHERE g.Id = s.Id);

UPDATE g
SET    g.Name = s.Name, g.Description = s.Description, g.Category = s.Category,
       g.IsActive = s.IsActive, g.SortOrder = s.SortOrder
FROM   [{prefix}_billing].features g
JOIN   billing.features s ON s.Id = g.Id;

-- Plan-feature matrix: insert missing AND sync IsEnabled/LimitValue so toggling
-- a row in billing.plan_features (admin UI or BillingSeeder edit) flows down.
INSERT INTO [{prefix}_billing].plan_features (Id, PlanId, FeatureId, IsEnabled, LimitValue)
SELECT Id, PlanId, FeatureId, IsEnabled, LimitValue
FROM billing.plan_features s
WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].plan_features g WHERE g.PlanId = s.PlanId AND g.FeatureId = s.FeatureId);

UPDATE g
SET    g.IsEnabled = s.IsEnabled, g.LimitValue = s.LimitValue
FROM   [{prefix}_billing].plan_features g
JOIN   billing.plan_features s ON s.PlanId = g.PlanId AND s.FeatureId = g.FeatureId;

IF EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas sc ON sc.schema_id=t.schema_id WHERE sc.name='{prefix}_billing' AND t.name='plan_storage_limits')
BEGIN
    INSERT INTO [{prefix}_billing].plan_storage_limits (Id, PlanId, ResourceType, LimitValue)
    SELECT Id, PlanId, ResourceType, LimitValue
    FROM billing.plan_storage_limits s
    WHERE NOT EXISTS (SELECT 1 FROM [{prefix}_billing].plan_storage_limits g WHERE g.PlanId = s.PlanId AND g.ResourceType = s.ResourceType);

    UPDATE g
    SET    g.LimitValue = s.LimitValue
    FROM   [{prefix}_billing].plan_storage_limits g
    JOIN   billing.plan_storage_limits s ON s.PlanId = g.PlanId AND s.ResourceType = g.ResourceType;
END

DECLARE @planId uniqueidentifier;
DECLARE @ownerTier nvarchar(50) = (SELECT TOP 1 PlanTier FROM [identity].contracts WHERE OwnerId = (SELECT TOP 1 Id FROM [identity].owners WHERE UserId = @ownerUserId));
DECLARE @tierCode nvarchar(50) = LOWER(ISNULL(@ownerTier, N'free'));
-- Legacy admin DTOs expose ""Enterprise"" as a tier label. The actual plan
-- catalog uses ""business"" for the top tier; alias them so a gym whose owner
-- contract says ""Enterprise"" still resolves to the Business plan instead of
-- silently falling through to Free (which crushes the gym to a 1-trainer cap).
IF @tierCode = N'enterprise' SET @tierCode = N'business';
SELECT @planId = Id FROM [{prefix}_billing].subscription_plans WHERE IsActive = 1 AND Code = @tierCode;
IF @planId IS NULL
    SELECT @planId = Id FROM [{prefix}_billing].subscription_plans WHERE IsActive = 1 AND Code = N'free';

DECLARE @now datetime2 = SYSUTCDATETIME();
IF @planId IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM [{prefix}_billing].gym_subscriptions WHERE GymId = @gymId)
    BEGIN
        -- Repair orphan subscriptions: PlanId points to a wiped plan, sub is inactive,
        -- or expired. Without this, post-wipe boots would leave gyms stranded on a
        -- bad PlanId and IsFeatureEnabledAsync silently returns false.
        UPDATE s
        SET    PlanId        = @planId,
               IsActive      = 1,
               PaymentStatus = N'Paid',
               ExpiresAt     = DATEADD(YEAR, 1, @now),
               LastPaymentAt = @now,
               NextPaymentDue= DATEADD(MONTH, 1, @now),
               OverdueSince  = NULL,
               GracePeriodEndsAt = NULL
        FROM   [{prefix}_billing].gym_subscriptions s
        WHERE  s.GymId = @gymId
          AND  (NOT EXISTS (SELECT 1 FROM [{prefix}_billing].subscription_plans p WHERE p.Id = s.PlanId)
                OR s.IsActive = 0
                OR s.ExpiresAt < @now);
    END
    ELSE IF @ownerTier IS NOT NULL
    BEGIN
        -- Only seed a default subscription when the owner contract dictates a tier.
        -- For admin-registered gyms with no contract, leave the gym subscription-less
        -- so GymApprovedIntegrationEventHandler can subscribe it to the default Pro
        -- plan; otherwise this insert wins the race and locks the gym to Free.
        INSERT INTO [{prefix}_billing].gym_subscriptions
          (Id, GymId, PlanId, StartedAt, ExpiresAt, IsActive, IsTrial, TrialExpiresAt, PaymentStatus, LastPaymentAt, NextPaymentDue, OverdueSince, GracePeriodEndsAt)
        VALUES
          (NEWID(), @gymId, @planId, @now, DATEADD(YEAR, 1, @now), 1, 0, NULL, N'Paid', @now, DATEADD(MONTH, 1, @now), NULL, NULL);
    END
END

COMMIT;";

    /// <summary>The script with every schema reference resolved to the given (validated) prefix.</summary>
    public static string For(string prefix) => Template.Replace(PrefixToken, prefix);
}
