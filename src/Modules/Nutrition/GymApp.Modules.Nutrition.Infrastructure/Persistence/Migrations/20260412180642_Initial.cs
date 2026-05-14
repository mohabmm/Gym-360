using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Nutrition.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nutrition");

            migrationBuilder.CreateTable(
                name: "food_database",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServingSize = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ServingUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CaloriesPerServing = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinPerServing = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CarbsPerServing = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    FatPerServing = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_database", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "food_logs",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_calories = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    total_protein = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    total_carbs = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    total_fat = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_food_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meal_plans",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DailyCalorieTarget = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    macro_protein_grams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    macro_carbs_grams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    macro_fat_grams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "water_logs",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalMl = table.Column<int>(type: "int", nullable: false),
                    TargetMl = table.Column<int>(type: "int", nullable: false, defaultValue: 2000),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_water_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meal_entries",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalCalories = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    food_items = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_entries_food_logs_FoodLogId",
                        column: x => x.FoodLogId,
                        principalSchema: "nutrition",
                        principalTable: "food_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_plan_days",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    alternatives = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    planned_meals = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plan_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meal_plan_days_meal_plans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalSchema: "nutrition",
                        principalTable: "meal_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "water_entries",
                schema: "nutrition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaterLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountMl = table.Column<int>(type: "int", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_water_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_water_entries_water_logs_WaterLogId",
                        column: x => x.WaterLogId,
                        principalSchema: "nutrition",
                        principalTable: "water_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_food_database_category",
                schema: "nutrition",
                table: "food_database",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_food_database_name",
                schema: "nutrition",
                table: "food_database",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_food_database_name_arabic",
                schema: "nutrition",
                table: "food_database",
                column: "NameArabic");

            migrationBuilder.CreateIndex(
                name: "IX_food_logs_user_date",
                schema: "nutrition",
                table: "food_logs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_food_logs_user_gym",
                schema: "nutrition",
                table: "food_logs",
                columns: new[] { "UserId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_entries_food_log",
                schema: "nutrition",
                table: "meal_entries",
                column: "FoodLogId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_days_meal_plan",
                schema: "nutrition",
                table: "meal_plan_days",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plans_client_gym",
                schema: "nutrition",
                table: "meal_plans",
                columns: new[] { "ClientId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_plans_trainer_gym",
                schema: "nutrition",
                table: "meal_plans",
                columns: new[] { "TrainerId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "nutrition",
                table: "outbox_messages",
                column: "ProcessedOnUtc",
                filter: "[ProcessedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_water_entries_water_log",
                schema: "nutrition",
                table: "water_entries",
                column: "WaterLogId");

            migrationBuilder.CreateIndex(
                name: "IX_water_logs_user_date",
                schema: "nutrition",
                table: "water_logs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_water_logs_user_gym",
                schema: "nutrition",
                table: "water_logs",
                columns: new[] { "UserId", "GymId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "food_database",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_entries",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_plan_days",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "water_entries",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "food_logs",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "meal_plans",
                schema: "nutrition");

            migrationBuilder.DropTable(
                name: "water_logs",
                schema: "nutrition");
        }
    }
}
