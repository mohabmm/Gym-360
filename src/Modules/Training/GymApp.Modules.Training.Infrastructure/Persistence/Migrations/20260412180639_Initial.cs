using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.CreateTable(
                name: "exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DemoMediaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    muscle_groups = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "programs",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationWeeks = table.Column<int>(type: "int", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "remote_workout_configs",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_remote_workout_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workout_assignments",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRemoteMode = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workout_logs",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    muscle_groups_worked = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PerceivedExertion = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    target_muscle_groups = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_weeks",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    day_workouts = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_weeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_weeks_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalSchema: "training",
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "log_sets",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WeightUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_log_sets_workout_logs_WorkoutLogId",
                        column: x => x.WorkoutLogId,
                        principalSchema: "training",
                        principalTable: "workout_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Sets = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupersetGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_exercises_workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalSchema: "training",
                        principalTable: "workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "training",
                table: "exercises",
                columns: new[] { "Id", "Category", "CreatedBy", "DemoMediaUrl", "Description", "Equipment", "GymId", "Instructions", "IsActive", "muscle_groups", "Name", "Scope" },
                values: new object[,]
                {
                    { new Guid("a1b2c300-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Bodyweight", null, null, true, "Cardio", "Jumping Jacks", "Global" },
                    { new Guid("a1b2c301-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "None", null, null, true, "Cardio", "Sprints", "Global" },
                    { new Guid("a1b2c302-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "None", null, null, true, "Cardio", "Outdoor Running", "Global" },
                    { new Guid("a1b2c303-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "None", null, null, true, "Cardio", "Outdoor Cycling", "Global" },
                    { new Guid("a1b2c304-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Other", null, null, true, "Cardio", "Jump Rope", "Global" },
                    { new Guid("a1b2c305-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Other", null, null, true, "Cardio", "Double Unders", "Global" },
                    { new Guid("a1b2c306-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Bodyweight", null, null, true, "Cardio", "Burpees", "Global" },
                    { new Guid("a1b2c307-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Bodyweight", null, null, true, "Cardio", "High Knees", "Global" },
                    { new Guid("a1b2c308-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Hip Flexor Stretch", "Global" },
                    { new Guid("a1b2c309-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Chest Stretch (Doorway)", "Global" },
                    { new Guid("a1b2c30a-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Shoulder Stretch (Cross-Body)", "Global" },
                    { new Guid("a1b2c30b-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Tricep Stretch (Overhead)", "Global" },
                    { new Guid("a1b2c30c-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "None", null, null, true, "Cardio", "Swimming", "Global" },
                    { new Guid("a1b2c30d-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Other", null, null, true, "Cardio", "Battle Ropes", "Global" },
                    { new Guid("a1b2c30e-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Hamstring Stretch", "Global" },
                    { new Guid("a1b2c30f-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Quad Stretch", "Global" },
                    { new Guid("a1b2c310-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Hollow Body Hold", "Global" },
                    { new Guid("a1b2c311-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Treadmill Running", "Global" },
                    { new Guid("a1b2c312-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Treadmill Walking", "Global" },
                    { new Guid("a1b2c313-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Treadmill Incline Walking", "Global" },
                    { new Guid("a1b2c314-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Decline Sit-Ups", "Global" },
                    { new Guid("a1b2c315-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Dragon Flag", "Global" },
                    { new Guid("a1b2c316-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Flutter Kicks", "Global" },
                    { new Guid("a1b2c317-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Toe Touches", "Global" },
                    { new Guid("a1b2c318-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Stair Climber", "Global" },
                    { new Guid("a1b2c319-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "StairMaster", "Global" },
                    { new Guid("a1b2c31a-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Assault Bike", "Global" },
                    { new Guid("a1b2c31b-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Ski Erg", "Global" },
                    { new Guid("a1b2c31c-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Stationary Bike", "Global" },
                    { new Guid("a1b2c31d-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Recumbent Bike", "Global" },
                    { new Guid("a1b2c31e-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Elliptical Trainer", "Global" },
                    { new Guid("a1b2c31f-e5f6-7890-abcd-ef1234567890"), "Cardio", null, null, null, "Machine", null, null, true, "Cardio", "Rowing Machine", "Global" },
                    { new Guid("a1b2c324-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Other", null, null, true, "FullBody", "Medicine Ball Chest Pass", "Global" },
                    { new Guid("a1b2c325-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Other", null, null, true, "FullBody", "Medicine Ball Rotational Throw", "Global" },
                    { new Guid("a1b2c326-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Bodyweight", null, null, true, "FullBody", "Skater Jump", "Global" },
                    { new Guid("a1b2c330-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Seated Forward Fold", "Global" },
                    { new Guid("a1b2c331-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Standing Calf Stretch", "Global" },
                    { new Guid("a1b2c332-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "Other", null, null, true, "FullBody", "Foam Rolling (Full Body)", "Global" },
                    { new Guid("a1b2c333-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "Other", null, null, true, "FullBody", "Foam Rolling (IT Band)", "Global" },
                    { new Guid("a1b2c334-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Lat Stretch", "Global" },
                    { new Guid("a1b2c335-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Cat-Cow Stretch", "Global" },
                    { new Guid("a1b2c336-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Child's Pose", "Global" },
                    { new Guid("a1b2c337-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "None", null, null, true, "FullBody", "Pigeon Pose", "Global" },
                    { new Guid("a1b2c338-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Bodyweight", null, null, true, "FullBody", "Tuck Jump", "Global" },
                    { new Guid("a1b2c339-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Bodyweight", null, null, true, "FullBody", "Lateral Bound", "Global" },
                    { new Guid("a1b2c33a-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Bodyweight", null, null, true, "FullBody", "Plyo Push-Up", "Global" },
                    { new Guid("a1b2c33b-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Other", null, null, true, "FullBody", "Medicine Ball Slam", "Global" },
                    { new Guid("a1b2c33c-e5f6-7890-abcd-ef1234567890"), "Flexibility", null, null, null, "Other", null, null, true, "FullBody", "Foam Rolling (Back)", "Global" },
                    { new Guid("a1b2c33d-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Other", null, null, true, "FullBody", "Box Jump", "Global" },
                    { new Guid("a1b2c33e-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Other", null, null, true, "FullBody", "Depth Jump", "Global" },
                    { new Guid("a1b2c33f-e5f6-7890-abcd-ef1234567890"), "Plyometric", null, null, null, "Bodyweight", null, null, true, "FullBody", "Broad Jump", "Global" },
                    { new Guid("a1b2c340-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Nordic Hamstring Curl", "Global" },
                    { new Guid("a1b2c341-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Legs", "Cable Pull-Through", "Global" },
                    { new Guid("a1b2c342-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Standing Calf Raise (Machine)", "Global" },
                    { new Guid("a1b2c343-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Seated Calf Raise (Machine)", "Global" },
                    { new Guid("a1b2c344-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Romanian Deadlift", "Global" },
                    { new Guid("a1b2c345-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Single-Leg Romanian Deadlift", "Global" },
                    { new Guid("a1b2c346-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Good Morning", "Global" },
                    { new Guid("a1b2c347-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Glute-Ham Raise", "Global" },
                    { new Guid("a1b2c348-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Calf Raise", "Global" },
                    { new Guid("a1b2c349-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Single-Leg Calf Raise", "Global" },
                    { new Guid("a1b2c34a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Bodyweight Calf Raise", "Global" },
                    { new Guid("a1b2c34b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Glutes", "Barbell Hip Thrust", "Global" },
                    { new Guid("a1b2c34c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Leg Press Calf Raise", "Global" },
                    { new Guid("a1b2c34d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Smith Machine Calf Raise", "Global" },
                    { new Guid("a1b2c34e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Donkey Calf Raise", "Global" },
                    { new Guid("a1b2c34f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Barbell Calf Raise", "Global" },
                    { new Guid("a1b2c350-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "V-Squat Machine", "Global" },
                    { new Guid("a1b2c351-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Pendulum Squat", "Global" },
                    { new Guid("a1b2c352-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Sissy Squat", "Global" },
                    { new Guid("a1b2c353-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Pistol Squat", "Global" },
                    { new Guid("a1b2c354-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Leg Press", "Global" },
                    { new Guid("a1b2c355-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Hack Squat Machine", "Global" },
                    { new Guid("a1b2c356-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Leg Extension Machine", "Global" },
                    { new Guid("a1b2c357-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Smith Machine Squat", "Global" },
                    { new Guid("a1b2c358-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Lying Leg Curl Machine", "Global" },
                    { new Guid("a1b2c359-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Seated Leg Curl Machine", "Global" },
                    { new Guid("a1b2c35a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Legs", "Standing Leg Curl Machine", "Global" },
                    { new Guid("a1b2c35b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Stiff-Leg Deadlift", "Global" },
                    { new Guid("a1b2c35c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Wall Sit", "Global" },
                    { new Guid("a1b2c35d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Jump Squat", "Global" },
                    { new Guid("a1b2c35e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Legs", "Box Jump", "Global" },
                    { new Guid("a1b2c35f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Legs", "Kettlebell Goblet Squat", "Global" },
                    { new Guid("a1b2c360-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Leg Raises (Lying)", "Global" },
                    { new Guid("a1b2c361-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Hanging Leg Raises", "Global" },
                    { new Guid("a1b2c362-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Hanging Knee Raises", "Global" },
                    { new Guid("a1b2c363-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Russian Twist", "Global" },
                    { new Guid("a1b2c364-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Bicycle Crunches", "Global" },
                    { new Guid("a1b2c365-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Reverse Crunches", "Global" },
                    { new Guid("a1b2c366-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Sit-Ups", "Global" },
                    { new Guid("a1b2c367-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "V-Ups", "Global" },
                    { new Guid("a1b2c368-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Core", "Cable Crunch", "Global" },
                    { new Guid("a1b2c369-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Core", "Cable Woodchop", "Global" },
                    { new Guid("a1b2c36a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Core", "Cable Pallof Press", "Global" },
                    { new Guid("a1b2c36b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Core", "Machine Crunch", "Global" },
                    { new Guid("a1b2c36c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Mountain Climbers", "Global" },
                    { new Guid("a1b2c36d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Dead Bug", "Global" },
                    { new Guid("a1b2c36e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Bird Dog", "Global" },
                    { new Guid("a1b2c36f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Other", null, null, true, "Core", "Ab Wheel Rollout", "Global" },
                    { new Guid("a1b2c370-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Glutes", "Cable Hip Abduction", "Global" },
                    { new Guid("a1b2c371-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Glutes", "Machine Hip Thrust", "Global" },
                    { new Guid("a1b2c372-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Glutes", "Machine Hip Abduction", "Global" },
                    { new Guid("a1b2c373-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Glutes", "Machine Hip Adduction", "Global" },
                    { new Guid("a1b2c374-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Glutes", "Dumbbell Hip Thrust", "Global" },
                    { new Guid("a1b2c375-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Glutes", "Glute Bridge", "Global" },
                    { new Guid("a1b2c376-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Glutes", "Single-Leg Glute Bridge", "Global" },
                    { new Guid("a1b2c377-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Glutes", "Cable Kickback", "Global" },
                    { new Guid("a1b2c378-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Glutes", "Frog Pump", "Global" },
                    { new Guid("a1b2c379-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Plank", "Global" },
                    { new Guid("a1b2c37a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Side Plank", "Global" },
                    { new Guid("a1b2c37b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Core", "Crunches", "Global" },
                    { new Guid("a1b2c37c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Band", null, null, true, "Glutes", "Banded Clamshell", "Global" },
                    { new Guid("a1b2c37d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Band", null, null, true, "Glutes", "Banded Lateral Walk", "Global" },
                    { new Guid("a1b2c37e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Glutes", "Sumo Squat", "Global" },
                    { new Guid("a1b2c37f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Glutes", "Kettlebell Sumo Deadlift", "Global" },
                    { new Guid("a1b2c380-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Hammer Curl", "Global" },
                    { new Guid("a1b2c381-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Incline Dumbbell Curl", "Global" },
                    { new Guid("a1b2c382-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Concentration Curl", "Global" },
                    { new Guid("a1b2c383-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Zottman Curl", "Global" },
                    { new Guid("a1b2c384-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "Preacher Curl (Barbell)", "Global" },
                    { new Guid("a1b2c385-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "Reverse Barbell Curl", "Global" },
                    { new Guid("a1b2c386-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "Drag Curl", "Global" },
                    { new Guid("a1b2c387-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Dumbbell Curl", "Global" },
                    { new Guid("a1b2c388-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Biceps", "High Cable Curl", "Global" },
                    { new Guid("a1b2c389-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Biceps", "Machine Preacher Curl", "Global" },
                    { new Guid("a1b2c38a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Biceps", "Machine Bicep Curl", "Global" },
                    { new Guid("a1b2c38b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Spider Curl", "Global" },
                    { new Guid("a1b2c38c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Cross-Body Hammer Curl", "Global" },
                    { new Guid("a1b2c38d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Biceps", "Preacher Curl (Dumbbell)", "Global" },
                    { new Guid("a1b2c38e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Biceps", "Cable Curl", "Global" },
                    { new Guid("a1b2c38f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Biceps", "Cable Hammer Curl (Rope)", "Global" },
                    { new Guid("a1b2c390-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Shoulders", "Machine Shoulder Press", "Global" },
                    { new Guid("a1b2c391-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Shoulders", "Machine Lateral Raise", "Global" },
                    { new Guid("a1b2c392-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Shoulders", "Machine Rear Delt Fly", "Global" },
                    { new Guid("a1b2c393-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Shoulders", "Smith Machine Shoulder Press", "Global" },
                    { new Guid("a1b2c394-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Shoulders", "Cable Lateral Raise", "Global" },
                    { new Guid("a1b2c395-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Shoulders", "Cable Front Raise", "Global" },
                    { new Guid("a1b2c396-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Shoulders", "Cable Face Pull", "Global" },
                    { new Guid("a1b2c397-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Shoulders", "Cable Rear Delt Fly", "Global" },
                    { new Guid("a1b2c398-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Band", null, null, true, "Shoulders", "Band Pull-Apart", "Global" },
                    { new Guid("a1b2c399-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Shoulders", "Kettlebell Press", "Global" },
                    { new Guid("a1b2c39a-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "Barbell Curl", "Global" },
                    { new Guid("a1b2c39b-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "EZ-Bar Curl", "Global" },
                    { new Guid("a1b2c39c-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Shoulders", "Landmine Press", "Global" },
                    { new Guid("a1b2c39d-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Shoulders", "Pike Push-Ups", "Global" },
                    { new Guid("a1b2c39e-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Shoulders", "Handstand Push-Ups", "Global" },
                    { new Guid("a1b2c39f-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Other", null, null, true, "Shoulders", "Plate Front Raise", "Global" },
                    { new Guid("a1b2c3a0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Barbell Back Squat", "Global" },
                    { new Guid("a1b2c3a1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Barbell Front Squat", "Global" },
                    { new Guid("a1b2c3a2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Overhead Squat", "Global" },
                    { new Guid("a1b2c3a3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Zercher Squat", "Global" },
                    { new Guid("a1b2c3a4-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Triceps", "Machine Tricep Extension", "Global" },
                    { new Guid("a1b2c3a5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Triceps", "Machine Dip", "Global" },
                    { new Guid("a1b2c3a6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Triceps", "JM Press", "Global" },
                    { new Guid("a1b2c3a7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Triceps", "Kettlebell Tricep Extension", "Global" },
                    { new Guid("a1b2c3a8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Walking Lunge", "Global" },
                    { new Guid("a1b2c3a9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Step-Up", "Global" },
                    { new Guid("a1b2c3aa-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Bulgarian Split Squat", "Global" },
                    { new Guid("a1b2c3ab-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Goblet Squat", "Global" },
                    { new Guid("a1b2c3ac-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Barbell Lunge", "Global" },
                    { new Guid("a1b2c3ad-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Legs", "Barbell Step-Up", "Global" },
                    { new Guid("a1b2c3ae-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Squat", "Global" },
                    { new Guid("a1b2c3af-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Legs", "Dumbbell Lunge", "Global" },
                    { new Guid("a1b2c3b0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Triceps", "Overhead Cable Tricep Extension", "Global" },
                    { new Guid("a1b2c3b1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Triceps", "Single-Arm Cable Pushdown", "Global" },
                    { new Guid("a1b2c3b2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Triceps", "Skull Crusher (EZ-Bar)", "Global" },
                    { new Guid("a1b2c3b3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Triceps", "Close-Grip Barbell Bench Press", "Global" },
                    { new Guid("a1b2c3b4-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Biceps", "21s Curl", "Global" },
                    { new Guid("a1b2c3b5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Biceps", "Kettlebell Curl", "Global" },
                    { new Guid("a1b2c3b6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Triceps", "Tricep Pushdown (Cable)", "Global" },
                    { new Guid("a1b2c3b7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Triceps", "Tricep Rope Pushdown", "Global" },
                    { new Guid("a1b2c3b8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Triceps", "Tate Press", "Global" },
                    { new Guid("a1b2c3b9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Triceps", "Tricep Dips", "Global" },
                    { new Guid("a1b2c3ba-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Triceps", "Bench Dips", "Global" },
                    { new Guid("a1b2c3bb-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Triceps", "Diamond Push-Ups (Triceps)", "Global" },
                    { new Guid("a1b2c3bc-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Triceps", "Barbell Overhead Tricep Extension", "Global" },
                    { new Guid("a1b2c3bd-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Triceps", "Dumbbell Overhead Tricep Extension", "Global" },
                    { new Guid("a1b2c3be-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Triceps", "Dumbbell Kickback", "Global" },
                    { new Guid("a1b2c3bf-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Triceps", "Dumbbell Skull Crusher", "Global" },
                    { new Guid("a1b2c3c0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Chest", "Wide Push-Ups", "Global" },
                    { new Guid("a1b2c3c1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Chest", "Diamond Push-Ups", "Global" },
                    { new Guid("a1b2c3c2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Chest", "Decline Push-Ups", "Global" },
                    { new Guid("a1b2c3c3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Chest", "Chest Dips", "Global" },
                    { new Guid("a1b2c3c4-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Chest", "Machine Incline Chest Press", "Global" },
                    { new Guid("a1b2c3c5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Chest", "Pec Deck Machine", "Global" },
                    { new Guid("a1b2c3c6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Chest", "Smith Machine Bench Press", "Global" },
                    { new Guid("a1b2c3c7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Chest", "Push-Ups", "Global" },
                    { new Guid("a1b2c3c8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Bent-Over Barbell Row", "Global" },
                    { new Guid("a1b2c3c9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Pendlay Row", "Global" },
                    { new Guid("a1b2c3ca-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "T-Bar Row", "Global" },
                    { new Guid("a1b2c3cb-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Back", "Dumbbell Row", "Global" },
                    { new Guid("a1b2c3cc-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Svend Press", "Global" },
                    { new Guid("a1b2c3cd-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Conventional Deadlift", "Global" },
                    { new Guid("a1b2c3ce-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Sumo Deadlift", "Global" },
                    { new Guid("a1b2c3cf-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Romanian Deadlift", "Global" },
                    { new Guid("a1b2c3d0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Chest", "Close-Grip Bench Press", "Global" },
                    { new Guid("a1b2c3d1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Dumbbell Bench Press", "Global" },
                    { new Guid("a1b2c3d2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Incline Dumbbell Bench Press", "Global" },
                    { new Guid("a1b2c3d3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Decline Dumbbell Bench Press", "Global" },
                    { new Guid("a1b2c3d5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Chest", "Barbell Bench Press", "Global" },
                    { new Guid("a1b2c3d6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Chest", "Incline Barbell Bench Press", "Global" },
                    { new Guid("a1b2c3d7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Chest", "Decline Barbell Bench Press", "Global" },
                    { new Guid("a1b2c3d8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Chest", "Cable Crossover", "Global" },
                    { new Guid("a1b2c3d9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Chest", "Low Cable Crossover", "Global" },
                    { new Guid("a1b2c3da-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Chest", "Cable Fly", "Global" },
                    { new Guid("a1b2c3db-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Chest", "Machine Chest Press", "Global" },
                    { new Guid("a1b2c3dc-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Dumbbell Fly", "Global" },
                    { new Guid("a1b2c3dd-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Incline Dumbbell Fly", "Global" },
                    { new Guid("a1b2c3de-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Decline Dumbbell Fly", "Global" },
                    { new Guid("a1b2c3df-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Chest", "Dumbbell Pullover", "Global" },
                    { new Guid("a1b2c3e0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Kettlebell", null, null, true, "Back", "Kettlebell Swing", "Global" },
                    { new Guid("a1b2c3e1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Shoulders", "Overhead Press (Military Press)", "Global" },
                    { new Guid("a1b2c3e2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Shoulders", "Seated Barbell Press", "Global" },
                    { new Guid("a1b2c3e3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Shoulders", "Push Press", "Global" },
                    { new Guid("a1b2c3e4-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Back", "Neutral-Grip Pull-Ups", "Global" },
                    { new Guid("a1b2c3e5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Back", "Inverted Row", "Global" },
                    { new Guid("a1b2c3e6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Back", "Back Extension", "Global" },
                    { new Guid("a1b2c3e7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Back", "Hyperextension", "Global" },
                    { new Guid("a1b2c3e8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Dumbbell Lateral Raise", "Global" },
                    { new Guid("a1b2c3e9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Dumbbell Front Raise", "Global" },
                    { new Guid("a1b2c3ea-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Dumbbell Rear Delt Fly", "Global" },
                    { new Guid("a1b2c3eb-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Dumbbell Upright Row", "Global" },
                    { new Guid("a1b2c3ec-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Shoulders", "Barbell Upright Row", "Global" },
                    { new Guid("a1b2c3ed-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Dumbbell Shoulder Press", "Global" },
                    { new Guid("a1b2c3ee-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Seated Dumbbell Press", "Global" },
                    { new Guid("a1b2c3ef-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Shoulders", "Arnold Press", "Global" },
                    { new Guid("a1b2c3f0-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Wide-Grip Lat Pulldown", "Global" },
                    { new Guid("a1b2c3f1-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Close-Grip Lat Pulldown", "Global" },
                    { new Guid("a1b2c3f2-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Reverse-Grip Lat Pulldown", "Global" },
                    { new Guid("a1b2c3f3-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Seated Cable Row", "Global" },
                    { new Guid("a1b2c3f4-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Back", "Dumbbell Deadlift", "Global" },
                    { new Guid("a1b2c3f5-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Dumbbell", null, null, true, "Back", "Dumbbell Shrug", "Global" },
                    { new Guid("a1b2c3f6-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Barbell", null, null, true, "Back", "Barbell Shrug", "Global" },
                    { new Guid("a1b2c3f7-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Lat Pulldown", "Global" },
                    { new Guid("a1b2c3f8-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Back", "Chest-Supported Row Machine", "Global" },
                    { new Guid("a1b2c3f9-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Back", "Smith Machine Bent-Over Row", "Global" },
                    { new Guid("a1b2c3fa-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Back", "Pull-Ups", "Global" },
                    { new Guid("a1b2c3fb-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Bodyweight", null, null, true, "Back", "Chin-Ups", "Global" },
                    { new Guid("a1b2c3fc-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Single-Arm Cable Row", "Global" },
                    { new Guid("a1b2c3fd-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Cable", null, null, true, "Back", "Straight-Arm Pulldown", "Global" },
                    { new Guid("a1b2c3fe-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Back", "Machine Row", "Global" },
                    { new Guid("a1b2c3ff-e5f6-7890-abcd-ef1234567890"), "Strength", null, null, null, "Machine", null, null, true, "Back", "Machine Lat Pulldown", "Global" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Category",
                schema: "training",
                table: "exercises",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_GymId",
                schema: "training",
                table: "exercises",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Name_GymId",
                schema: "training",
                table: "exercises",
                columns: new[] { "Name", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Scope",
                schema: "training",
                table: "exercises",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_log_sets_WorkoutLogId_ExerciseId_SetNumber",
                schema: "training",
                table: "log_sets",
                columns: new[] { "WorkoutLogId", "ExerciseId", "SetNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_program_weeks_ProgramId_WeekNumber",
                schema: "training",
                table: "program_weeks",
                columns: new[] { "ProgramId", "WeekNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_programs_IsTemplate",
                schema: "training",
                table: "programs",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_programs_TrainerId_GymId",
                schema: "training",
                table: "programs",
                columns: new[] { "TrainerId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_remote_workout_configs_ClientId_GymId_IsActive",
                schema: "training",
                table: "remote_workout_configs",
                columns: new[] { "ClientId", "GymId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_remote_workout_configs_TrainerId_ClientId_GymId",
                schema: "training",
                table: "remote_workout_configs",
                columns: new[] { "TrainerId", "ClientId", "GymId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_assignments_ClientId_GymId_Status",
                schema: "training",
                table: "workout_assignments",
                columns: new[] { "ClientId", "GymId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_assignments_TrainerId_ClientId_GymId",
                schema: "training",
                table: "workout_assignments",
                columns: new[] { "TrainerId", "ClientId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_WorkoutId_Order",
                schema: "training",
                table: "workout_exercises",
                columns: new[] { "WorkoutId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_UserId_Date_WorkoutId",
                schema: "training",
                table: "workout_logs",
                columns: new[] { "UserId", "Date", "WorkoutId" },
                unique: true,
                filter: "[WorkoutId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_UserId_GymId",
                schema: "training",
                table: "workout_logs",
                columns: new[] { "UserId", "GymId" });

            migrationBuilder.CreateIndex(
                name: "IX_workouts_IsTemplate",
                schema: "training",
                table: "workouts",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_workouts_TrainerId_GymId",
                schema: "training",
                table: "workouts",
                columns: new[] { "TrainerId", "GymId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "log_sets",
                schema: "training");

            migrationBuilder.DropTable(
                name: "program_weeks",
                schema: "training");

            migrationBuilder.DropTable(
                name: "remote_workout_configs",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_assignments",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_exercises",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workout_logs",
                schema: "training");

            migrationBuilder.DropTable(
                name: "programs",
                schema: "training");

            migrationBuilder.DropTable(
                name: "workouts",
                schema: "training");
        }
    }
}
