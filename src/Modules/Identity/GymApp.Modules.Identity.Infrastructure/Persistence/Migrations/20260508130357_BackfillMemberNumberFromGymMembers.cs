using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Backfills <c>identity.user_gym_memberships.member_number</c> from per-tenant
    /// <c>{prefix}_gyms.gym_members.MemberNumber</c>. Required because earlier versions
    /// of <c>AddMemberCommandHandler</c> discarded the auto-generated number returned
    /// by <c>IGymMemberDirectory.RegisterAsync</c>, leaving the Identity-side member
    /// number null and breaking the trainee app QR check-in screen.
    ///
    /// Iterates every schema matching <c>g%_gyms</c> (regex-validated tenant prefix
    /// at registry insert: <c>^g\d+$</c>) so this works regardless of how many gyms
    /// exist. Idempotent: only updates rows where <c>member_number IS NULL</c>.
    /// </summary>
    public partial class BackfillMemberNumberFromGymMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @schema sysname;
DECLARE @sql nvarchar(max);

DECLARE schema_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name
    FROM sys.schemas s
    INNER JOIN sys.tables t ON t.schema_id = s.schema_id
    WHERE s.name LIKE 'g%[_]gyms'
      AND s.name LIKE 'g[0-9]%[_]gyms'
      AND t.name = 'gym_members';

OPEN schema_cursor;
FETCH NEXT FROM schema_cursor INTO @schema;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'
        UPDATE m
        SET m.member_number = gm.[MemberNumber]
        FROM [identity].[user_gym_memberships] m
        INNER JOIN ' + QUOTENAME(@schema) + N'.[gym_members] gm
            ON gm.[MemberId] = m.[UserId] AND gm.[GymId] = m.[gym_id]
        WHERE m.[member_number] IS NULL
          AND gm.[MemberNumber] IS NOT NULL;';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM schema_cursor INTO @schema;
END;

CLOSE schema_cursor;
DEALLOCATE schema_cursor;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill — not reversible. Down intentionally empty.
        }
    }
}
