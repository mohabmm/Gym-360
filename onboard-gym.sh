#!/usr/bin/env bash
# =============================================================================
# onboard-gym.sh  —  One-shot onboarding for a new gym client.
#
# What it does (idempotent, safe to re-run):
#   1. Registers the gym owner via POST /api/v1/identity/register (BCrypt-hashed
#      password, proper PDPL consent) — skipped if email already exists.
#   2. Reads back the owner's UserId from SQL.
#   3. Creates the gym row, subscription (plan by code), one gym section, and
#      a default session-cancellation policy, and links the owner as GymOwner
#      in user_gym_memberships.
#
# Prerequisites:
#   - docker-compose up -d   (API on :8080, gymapp-db container running)
#   - seed-demo.sql has been run at least once so the 'demo-pro' plan and all
#     17 feature codes exist.  Or pass PLAN_CODE of an existing plan.
#
# Usage — edit the CONFIG block below, then:
#   chmod +x onboard-gym.sh
#   ./onboard-gym.sh
# =============================================================================
set -euo pipefail

# -----------------------------------------------------------------------------
# CONFIG  — edit these for each new gym
# -----------------------------------------------------------------------------
GYM_NAME="First Gym"
GYM_DESCRIPTION="Premium fitness club in New Cairo"

# Owner (a User row with role=GymOwner will be created via the API)
OWNER_EMAIL="${OWNER_EMAIL:-owner@firstgym.com}"
OWNER_PASSWORD="${OWNER_PASSWORD:?Set OWNER_PASSWORD env var before running (e.g. export OWNER_PASSWORD='Strong!Pass123')}"
OWNER_FIRST_NAME="${OWNER_FIRST_NAME:-Ahmed}"
OWNER_LAST_NAME="${OWNER_LAST_NAME:-Hassan}"
OWNER_PHONE="${OWNER_PHONE:-+201001234567}"   # E.164

# Contact on the gym record itself
GYM_CONTACT_EMAIL="info@firstgym.com"
GYM_CONTACT_PHONE="+20221234567"
GYM_WEBSITE=""                      # optional, empty = NULL

# Address
ADDR_STREET="15 El Thawra St"
ADDR_CITY="Cairo"
ADDR_STATE="Cairo"
ADDR_COUNTRY="Egypt"
ADDR_ZIP="11511"

# 6-char uppercase code shown to members when they self-enrol.
# Must be unique across all gyms. Leave blank to auto-generate.
GYM_CODE=""

# Subscription plan — must exist in billing.subscription_plans.Code
PLAN_CODE="demo-pro"

# Default gym section (Mens | Womens | Mixed)
SECTION_TYPE="Mixed"
SECTION_NAME="Main Floor"

# Session cancel / no-show policy
CANCEL_WINDOW_HOURS=24
NOSHOW_THRESHOLD=3
NOSHOW_WINDOW_DAYS=30
NOSHOW_ACTION="Warn"                # Warn | Block

# Endpoints / DB access — rarely need to change
API_URL="${API_URL:-http://localhost:8080}"
DB_CONTAINER="${DB_CONTAINER:-gymapp-db}"
DB_NAME="${DB_NAME:-GymApp}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-GymApp_P@ss2024!}"

# -----------------------------------------------------------------------------
# Helpers
# -----------------------------------------------------------------------------
say() { printf "\n\033[1;36m== %s ==\033[0m\n" "$*"; }
die() { printf "\n\033[1;31mERROR:\033[0m %s\n" "$*" >&2; exit 1; }

sqlcmd() {
  docker exec -i "$DB_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U "$DB_USER" -P "$DB_PASSWORD" -C -d "$DB_NAME" \
    -b -h -1 -W "$@"
}

# Auto-generate a 6-char uppercase code if caller left it blank.
if [[ -z "$GYM_CODE" ]]; then
  GYM_CODE=$(LC_ALL=C tr -dc 'A-Z0-9' </dev/urandom | head -c 6)
fi

# -----------------------------------------------------------------------------
# 1. Register the owner via the API (BCrypt hashing happens server-side).
# -----------------------------------------------------------------------------
say "Registering owner $OWNER_EMAIL"

REG_BODY=$(cat <<JSON
{
  "email": "$OWNER_EMAIL",
  "password": "$OWNER_PASSWORD",
  "firstName": "$OWNER_FIRST_NAME",
  "lastName": "$OWNER_LAST_NAME",
  "phone": "$OWNER_PHONE",
  "role": "GymOwner",
  "dataProcessingConsent": true,
  "marketingConsent": false
}
JSON
)

HTTP_CODE=$(curl -sS -o /tmp/onboard-reg.json -w "%{http_code}" \
  -X POST "$API_URL/api/v1/identity/register" \
  -H "Content-Type: application/json" \
  -d "$REG_BODY" || true)

case "$HTTP_CODE" in
  201) echo "  owner created." ;;
  400) echo "  owner already exists (or validation rejected) — continuing." ;;
  *)   echo "  unexpected HTTP $HTTP_CODE:"; cat /tmp/onboard-reg.json; echo
       die "registration failed" ;;
esac

# -----------------------------------------------------------------------------
# 2. Run the onboarding SQL. Idempotent everywhere.
# -----------------------------------------------------------------------------
say "Provisioning gym '$GYM_NAME'"

SQL=$(cat <<SQL
SET NOCOUNT ON;

DECLARE @OwnerEmail   nvarchar(256) = N'$OWNER_EMAIL';
DECLARE @GymName      nvarchar(200) = N'$GYM_NAME';
DECLARE @GymDesc      nvarchar(2000) = N'$GYM_DESCRIPTION';
DECLARE @GymCode      nvarchar(6)   = N'$GYM_CODE';
DECLARE @ContactEmail nvarchar(256) = N'$GYM_CONTACT_EMAIL';
DECLARE @ContactPhone nvarchar(50)  = N'$GYM_CONTACT_PHONE';
DECLARE @Website      nvarchar(500) = NULLIF(N'$GYM_WEBSITE','');
DECLARE @Street       nvarchar(500) = N'$ADDR_STREET';
DECLARE @City         nvarchar(100) = N'$ADDR_CITY';
DECLARE @State        nvarchar(100) = N'$ADDR_STATE';
DECLARE @Country      nvarchar(100) = N'$ADDR_COUNTRY';
DECLARE @Zip          nvarchar(20)  = N'$ADDR_ZIP';
DECLARE @PlanCode     nvarchar(50)  = N'$PLAN_CODE';
DECLARE @SectionType  nvarchar(20)  = N'$SECTION_TYPE';
DECLARE @SectionName  nvarchar(100) = N'$SECTION_NAME';
DECLARE @CancelHours  int = $CANCEL_WINDOW_HOURS;
DECLARE @NoshowTh     int = $NOSHOW_THRESHOLD;
DECLARE @NoshowDays   int = $NOSHOW_WINDOW_DAYS;
DECLARE @NoshowAction nvarchar(20)  = N'$NOSHOW_ACTION';
DECLARE @Now datetime2 = SYSUTCDATETIME();

-- Resolve owner
DECLARE @OwnerId uniqueidentifier;
SELECT @OwnerId = Id FROM [identity].users WHERE email = @OwnerEmail;
IF @OwnerId IS NULL
BEGIN
    RAISERROR('Owner user not found for %s — API registration must have failed.', 16, 1, @OwnerEmail);
    RETURN;
END

-- Resolve plan
DECLARE @PlanId uniqueidentifier;
SELECT @PlanId = Id FROM billing.subscription_plans WHERE Code = @PlanCode AND IsActive = 1;
IF @PlanId IS NULL
BEGIN
    RAISERROR('Plan not found: %s. Run seed-demo.sql first or adjust PLAN_CODE.', 16, 1, @PlanCode);
    RETURN;
END

-- Gym row (idempotent on Name + OwnerId, but primarily keyed by GymCode uniqueness)
DECLARE @GymId uniqueidentifier;
SELECT @GymId = Id FROM gyms.gyms WHERE GymCode = @GymCode OR (OwnerId = @OwnerId AND Name = @GymName);
IF @GymId IS NULL
BEGIN
    SET @GymId = NEWID();
    INSERT INTO gyms.gyms
      (Id, OwnerId, Name, Description,
       Address_Street, Address_City, Address_State, Address_Country, Address_ZipCode,
       Address_Latitude, Address_Longitude,
       Contact_Email, Contact_Phone, Contact_Website,
       Branding_LogoUrl, Branding_PrimaryColor, Branding_SecondaryColor, Branding_CoverPhotoUrl,
       GymCode,
       Social_Facebook, Social_Instagram, Social_Twitter, Social_Website,
       Status, PaymentStatus,
       Amenities, MemberIds, TrainerIds,
       CreatedAt, UpdatedAt)
    VALUES
      (@GymId, @OwnerId, @GymName, @GymDesc,
       @Street, @City, @State, @Country, @Zip,
       NULL, NULL,
       @ContactEmail, @ContactPhone, @Website,
       NULL, NULL, NULL, NULL,
       @GymCode,
       NULL, NULL, NULL, NULL,
       N'Active', N'Paid',
       N'', N'', N'',
       @Now, @Now);
    PRINT CONCAT('Gym created: ', CAST(@GymId AS nvarchar(40)));
END
ELSE
    PRINT CONCAT('Gym already exists: ', CAST(@GymId AS nvarchar(40)));

-- Subscription
IF NOT EXISTS (SELECT 1 FROM billing.gym_subscriptions WHERE GymId = @GymId AND IsActive = 1)
BEGIN
    INSERT INTO billing.gym_subscriptions
      (Id, GymId, PlanId, StartedAt, ExpiresAt,
       IsActive, IsTrial, TrialExpiresAt,
       PaymentStatus, LastPaymentAt, NextPaymentDue,
       OverdueSince, GracePeriodEndsAt)
    VALUES
      (NEWID(), @GymId, @PlanId, @Now, DATEADD(YEAR, 1, @Now),
       1, 0, NULL,
       N'Paid', @Now, DATEADD(MONTH, 1, @Now),
       NULL, NULL);
END

-- Section
IF NOT EXISTS (SELECT 1 FROM gyms.gym_sections WHERE GymId = @GymId AND Name = @SectionName)
    INSERT INTO gyms.gym_sections (Id, GymId, SectionType, Name, IsActive)
    VALUES (NEWID(), @GymId, @SectionType, @SectionName, 1);

-- Session policy
IF NOT EXISTS (SELECT 1 FROM gyms.gym_session_policies WHERE GymId = @GymId)
    INSERT INTO gyms.gym_session_policies
      (Id, GymId, CancellationWindowHours, NoshowThreshold, NoshowWindowDays, NoshowAction, UpdatedAt)
    VALUES
      (NEWID(), @GymId, @CancelHours, @NoshowTh, @NoshowDays, @NoshowAction, @Now);

-- Owner membership
IF NOT EXISTS (
    SELECT 1 FROM [identity].user_gym_memberships
    WHERE UserId = @OwnerId AND gym_id = @GymId)
BEGIN
    INSERT INTO [identity].user_gym_memberships
      (gym_id, role, joined_at, is_active, section, member_number, UserId)
    VALUES
      (@GymId, N'GymOwner', @Now, 1, NULL, NULL, @OwnerId);
END

-- Summary
SELECT 'gym_id'       AS k, CAST(@GymId   AS nvarchar(40)) AS v
UNION ALL SELECT 'gym_code',       @GymCode
UNION ALL SELECT 'owner_id',       CAST(@OwnerId AS nvarchar(40))
UNION ALL SELECT 'plan_code',      @PlanCode
UNION ALL SELECT 'section',        CONCAT(@SectionType, ' / ', @SectionName)
UNION ALL SELECT 'cancel_hours',   CAST(@CancelHours AS nvarchar(10))
UNION ALL SELECT 'api_url',        N'$API_URL';
SQL
)

echo "$SQL" | sqlcmd

say "Done. Login for the new owner:"
echo "  POST $API_URL/api/v1/identity/login"
echo "  { \"email\": \"$OWNER_EMAIL\", \"password\": \"$OWNER_PASSWORD\" }"
echo
echo "  Gym code (share with members):  $GYM_CODE"
