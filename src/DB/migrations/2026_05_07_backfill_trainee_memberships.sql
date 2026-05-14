-- Backfill identity.user_gym_memberships for trainees that were added
-- via Gyms.AddMember before the MemberAddedToGym integration-event handler
-- was wired up. Without this row the login response returns a null gymId,
-- the trainee Flutter app cannot send the X-Gym-Id header, and every
-- /me/* endpoint rejects the call with GYM_CONTEXT_REQUIRED.
--
-- Idempotent: re-running has no effect (LEFT JOIN guard).
-- Safe: skips users that already hold an active membership at any gym
--       (One-Gym principal rule, FOUNDER_DECISIONS Q3 / CLAUDE.md §11).
--
-- Run once on each environment that received pre-fix AddMember calls.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'
INSERT INTO identity.user_gym_memberships
    (gym_id, role, joined_at, is_active, section, member_number, [UserId])
SELECT m.GymId, N''Trainee'', m.JoinedAt, 1, m.Section, m.MemberNumber, m.MemberId
FROM '   + QUOTENAME(SchemaPrefix + N'_gyms') + N'.gym_members AS m
LEFT JOIN identity.user_gym_memberships AS existing
       ON existing.[UserId] = m.MemberId
      AND existing.gym_id  = m.GymId
      AND existing.role    = N''Trainee''
WHERE existing.Id IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM identity.user_gym_memberships other
      WHERE other.[UserId] = m.MemberId AND other.is_active = 1
  )
  AND EXISTS (
      SELECT 1 FROM identity.users u WHERE u.Id = m.MemberId
  );
'
FROM gyms_registry.gyms;

EXEC sp_executesql @sql;

COMMIT TRANSACTION;
