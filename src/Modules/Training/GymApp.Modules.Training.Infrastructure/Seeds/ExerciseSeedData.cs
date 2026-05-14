using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Infrastructure.Seeds;

internal static class ExerciseSeedData
{
    // Deterministic GUID namespace for exercises
    private static readonly Guid Namespace = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public static object[] GetExercises()
    {
        var exercises = new List<object>();
        int index = 0;

        // ─── CHEST ─────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Chest, ExerciseCategory.Strength, new[]
        {
            ("Barbell Bench Press", Equipment.Barbell),
            ("Incline Barbell Bench Press", Equipment.Barbell),
            ("Decline Barbell Bench Press", Equipment.Barbell),
            ("Close-Grip Bench Press", Equipment.Barbell),
            ("Dumbbell Bench Press", Equipment.Dumbbell),
            ("Incline Dumbbell Bench Press", Equipment.Dumbbell),
            ("Decline Dumbbell Bench Press", Equipment.Dumbbell),
            ("Dumbbell Fly", Equipment.Dumbbell),
            ("Incline Dumbbell Fly", Equipment.Dumbbell),
            ("Decline Dumbbell Fly", Equipment.Dumbbell),
            ("Dumbbell Pullover", Equipment.Dumbbell),
            ("Cable Crossover", Equipment.Cable),
            ("Low Cable Crossover", Equipment.Cable),
            ("Cable Fly", Equipment.Cable),
            ("Machine Chest Press", Equipment.Machine),
            ("Machine Incline Chest Press", Equipment.Machine),
            ("Pec Deck Machine", Equipment.Machine),
            ("Smith Machine Bench Press", Equipment.Machine),
            ("Push-Ups", Equipment.Bodyweight),
            ("Wide Push-Ups", Equipment.Bodyweight),
            ("Diamond Push-Ups", Equipment.Bodyweight),
            ("Decline Push-Ups", Equipment.Bodyweight),
            ("Chest Dips", Equipment.Bodyweight),
            ("Svend Press", Equipment.Dumbbell),
        }));

        // ─── BACK ──────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Back, ExerciseCategory.Strength, new[]
        {
            ("Conventional Deadlift", Equipment.Barbell),
            ("Sumo Deadlift", Equipment.Barbell),
            ("Romanian Deadlift", Equipment.Barbell),
            ("Bent-Over Barbell Row", Equipment.Barbell),
            ("Pendlay Row", Equipment.Barbell),
            ("T-Bar Row", Equipment.Barbell),
            ("Dumbbell Row", Equipment.Dumbbell),
            ("Dumbbell Deadlift", Equipment.Dumbbell),
            ("Dumbbell Shrug", Equipment.Dumbbell),
            ("Barbell Shrug", Equipment.Barbell),
            ("Lat Pulldown", Equipment.Cable),
            ("Wide-Grip Lat Pulldown", Equipment.Cable),
            ("Close-Grip Lat Pulldown", Equipment.Cable),
            ("Reverse-Grip Lat Pulldown", Equipment.Cable),
            ("Seated Cable Row", Equipment.Cable),
            ("Single-Arm Cable Row", Equipment.Cable),
            ("Straight-Arm Pulldown", Equipment.Cable),
            ("Machine Row", Equipment.Machine),
            ("Machine Lat Pulldown", Equipment.Machine),
            ("Chest-Supported Row Machine", Equipment.Machine),
            ("Smith Machine Bent-Over Row", Equipment.Machine),
            ("Pull-Ups", Equipment.Bodyweight),
            ("Chin-Ups", Equipment.Bodyweight),
            ("Neutral-Grip Pull-Ups", Equipment.Bodyweight),
            ("Inverted Row", Equipment.Bodyweight),
            ("Back Extension", Equipment.Bodyweight),
            ("Hyperextension", Equipment.Machine),
            ("Kettlebell Swing", Equipment.Kettlebell),
        }));

        // ─── SHOULDERS ─────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Shoulders, ExerciseCategory.Strength, new[]
        {
            ("Overhead Press (Military Press)", Equipment.Barbell),
            ("Seated Barbell Press", Equipment.Barbell),
            ("Push Press", Equipment.Barbell),
            ("Barbell Upright Row", Equipment.Barbell),
            ("Dumbbell Shoulder Press", Equipment.Dumbbell),
            ("Seated Dumbbell Press", Equipment.Dumbbell),
            ("Arnold Press", Equipment.Dumbbell),
            ("Dumbbell Lateral Raise", Equipment.Dumbbell),
            ("Dumbbell Front Raise", Equipment.Dumbbell),
            ("Dumbbell Rear Delt Fly", Equipment.Dumbbell),
            ("Dumbbell Upright Row", Equipment.Dumbbell),
            ("Cable Lateral Raise", Equipment.Cable),
            ("Cable Front Raise", Equipment.Cable),
            ("Cable Face Pull", Equipment.Cable),
            ("Cable Rear Delt Fly", Equipment.Cable),
            ("Machine Shoulder Press", Equipment.Machine),
            ("Machine Lateral Raise", Equipment.Machine),
            ("Machine Rear Delt Fly", Equipment.Machine),
            ("Smith Machine Shoulder Press", Equipment.Machine),
            ("Landmine Press", Equipment.Barbell),
            ("Pike Push-Ups", Equipment.Bodyweight),
            ("Handstand Push-Ups", Equipment.Bodyweight),
            ("Plate Front Raise", Equipment.Other),
            ("Band Pull-Apart", Equipment.Band),
            ("Kettlebell Press", Equipment.Kettlebell),
        }));

        // ─── BICEPS ────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Biceps, ExerciseCategory.Strength, new[]
        {
            ("Barbell Curl", Equipment.Barbell),
            ("EZ-Bar Curl", Equipment.Barbell),
            ("Preacher Curl (Barbell)", Equipment.Barbell),
            ("Reverse Barbell Curl", Equipment.Barbell),
            ("Drag Curl", Equipment.Barbell),
            ("Dumbbell Curl", Equipment.Dumbbell),
            ("Hammer Curl", Equipment.Dumbbell),
            ("Incline Dumbbell Curl", Equipment.Dumbbell),
            ("Concentration Curl", Equipment.Dumbbell),
            ("Zottman Curl", Equipment.Dumbbell),
            ("Cross-Body Hammer Curl", Equipment.Dumbbell),
            ("Preacher Curl (Dumbbell)", Equipment.Dumbbell),
            ("Cable Curl", Equipment.Cable),
            ("Cable Hammer Curl (Rope)", Equipment.Cable),
            ("High Cable Curl", Equipment.Cable),
            ("Machine Preacher Curl", Equipment.Machine),
            ("Machine Bicep Curl", Equipment.Machine),
            ("Spider Curl", Equipment.Dumbbell),
            ("21s Curl", Equipment.Barbell),
            ("Kettlebell Curl", Equipment.Kettlebell),
        }));

        // ─── TRICEPS ───────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Triceps, ExerciseCategory.Strength, new[]
        {
            ("Tricep Pushdown (Cable)", Equipment.Cable),
            ("Tricep Rope Pushdown", Equipment.Cable),
            ("Overhead Cable Tricep Extension", Equipment.Cable),
            ("Single-Arm Cable Pushdown", Equipment.Cable),
            ("Skull Crusher (EZ-Bar)", Equipment.Barbell),
            ("Close-Grip Barbell Bench Press", Equipment.Barbell),
            ("Barbell Overhead Tricep Extension", Equipment.Barbell),
            ("Dumbbell Overhead Tricep Extension", Equipment.Dumbbell),
            ("Dumbbell Kickback", Equipment.Dumbbell),
            ("Dumbbell Skull Crusher", Equipment.Dumbbell),
            ("Tate Press", Equipment.Dumbbell),
            ("Tricep Dips", Equipment.Bodyweight),
            ("Bench Dips", Equipment.Bodyweight),
            ("Diamond Push-Ups (Triceps)", Equipment.Bodyweight),
            ("Machine Tricep Extension", Equipment.Machine),
            ("Machine Dip", Equipment.Machine),
            ("JM Press", Equipment.Barbell),
            ("Kettlebell Tricep Extension", Equipment.Kettlebell),
        }));

        // ─── LEGS (QUADS) ──────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Legs, ExerciseCategory.Strength, new[]
        {
            ("Barbell Back Squat", Equipment.Barbell),
            ("Barbell Front Squat", Equipment.Barbell),
            ("Overhead Squat", Equipment.Barbell),
            ("Zercher Squat", Equipment.Barbell),
            ("Barbell Lunge", Equipment.Barbell),
            ("Barbell Step-Up", Equipment.Barbell),
            ("Dumbbell Squat", Equipment.Dumbbell),
            ("Dumbbell Lunge", Equipment.Dumbbell),
            ("Dumbbell Walking Lunge", Equipment.Dumbbell),
            ("Dumbbell Step-Up", Equipment.Dumbbell),
            ("Dumbbell Bulgarian Split Squat", Equipment.Dumbbell),
            ("Goblet Squat", Equipment.Dumbbell),
            ("Leg Press", Equipment.Machine),
            ("Hack Squat Machine", Equipment.Machine),
            ("Leg Extension Machine", Equipment.Machine),
            ("Smith Machine Squat", Equipment.Machine),
            ("V-Squat Machine", Equipment.Machine),
            ("Pendulum Squat", Equipment.Machine),
            ("Sissy Squat", Equipment.Bodyweight),
            ("Pistol Squat", Equipment.Bodyweight),
            ("Wall Sit", Equipment.Bodyweight),
            ("Jump Squat", Equipment.Bodyweight),
            ("Box Jump", Equipment.Bodyweight),
            ("Kettlebell Goblet Squat", Equipment.Kettlebell),
        }));

        // ─── LEGS (HAMSTRINGS) ─────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Legs, ExerciseCategory.Strength, new[]
        {
            ("Lying Leg Curl Machine", Equipment.Machine),
            ("Seated Leg Curl Machine", Equipment.Machine),
            ("Standing Leg Curl Machine", Equipment.Machine),
            ("Stiff-Leg Deadlift", Equipment.Barbell),
            ("Dumbbell Romanian Deadlift", Equipment.Dumbbell),
            ("Single-Leg Romanian Deadlift", Equipment.Dumbbell),
            ("Good Morning", Equipment.Barbell),
            ("Glute-Ham Raise", Equipment.Bodyweight),
            ("Nordic Hamstring Curl", Equipment.Bodyweight),
            ("Cable Pull-Through", Equipment.Cable),
        }));

        // ─── LEGS (CALVES) ─────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Legs, ExerciseCategory.Strength, new[]
        {
            ("Standing Calf Raise (Machine)", Equipment.Machine),
            ("Seated Calf Raise (Machine)", Equipment.Machine),
            ("Leg Press Calf Raise", Equipment.Machine),
            ("Smith Machine Calf Raise", Equipment.Machine),
            ("Donkey Calf Raise", Equipment.Machine),
            ("Barbell Calf Raise", Equipment.Barbell),
            ("Dumbbell Calf Raise", Equipment.Dumbbell),
            ("Single-Leg Calf Raise", Equipment.Bodyweight),
            ("Bodyweight Calf Raise", Equipment.Bodyweight),
        }));

        // ─── GLUTES ────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Glutes, ExerciseCategory.Strength, new[]
        {
            ("Barbell Hip Thrust", Equipment.Barbell),
            ("Dumbbell Hip Thrust", Equipment.Dumbbell),
            ("Glute Bridge", Equipment.Bodyweight),
            ("Single-Leg Glute Bridge", Equipment.Bodyweight),
            ("Cable Kickback", Equipment.Cable),
            ("Cable Hip Abduction", Equipment.Cable),
            ("Machine Hip Thrust", Equipment.Machine),
            ("Machine Hip Abduction", Equipment.Machine),
            ("Machine Hip Adduction", Equipment.Machine),
            ("Banded Clamshell", Equipment.Band),
            ("Banded Lateral Walk", Equipment.Band),
            ("Sumo Squat", Equipment.Dumbbell),
            ("Kettlebell Sumo Deadlift", Equipment.Kettlebell),
            ("Frog Pump", Equipment.Bodyweight),
        }));

        // ─── CORE ──────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Core, ExerciseCategory.Strength, new[]
        {
            ("Plank", Equipment.Bodyweight),
            ("Side Plank", Equipment.Bodyweight),
            ("Crunches", Equipment.Bodyweight),
            ("Bicycle Crunches", Equipment.Bodyweight),
            ("Reverse Crunches", Equipment.Bodyweight),
            ("Sit-Ups", Equipment.Bodyweight),
            ("V-Ups", Equipment.Bodyweight),
            ("Leg Raises (Lying)", Equipment.Bodyweight),
            ("Hanging Leg Raises", Equipment.Bodyweight),
            ("Hanging Knee Raises", Equipment.Bodyweight),
            ("Russian Twist", Equipment.Bodyweight),
            ("Mountain Climbers", Equipment.Bodyweight),
            ("Dead Bug", Equipment.Bodyweight),
            ("Bird Dog", Equipment.Bodyweight),
            ("Ab Wheel Rollout", Equipment.Other),
            ("Cable Crunch", Equipment.Cable),
            ("Cable Woodchop", Equipment.Cable),
            ("Cable Pallof Press", Equipment.Cable),
            ("Machine Crunch", Equipment.Machine),
            ("Decline Sit-Ups", Equipment.Bodyweight),
            ("Dragon Flag", Equipment.Bodyweight),
            ("Flutter Kicks", Equipment.Bodyweight),
            ("Toe Touches", Equipment.Bodyweight),
            ("Hollow Body Hold", Equipment.Bodyweight),
        }));

        // ─── CARDIO ────────────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.Cardio, ExerciseCategory.Cardio, new[]
        {
            ("Treadmill Running", Equipment.Machine),
            ("Treadmill Walking", Equipment.Machine),
            ("Treadmill Incline Walking", Equipment.Machine),
            ("Stationary Bike", Equipment.Machine),
            ("Recumbent Bike", Equipment.Machine),
            ("Elliptical Trainer", Equipment.Machine),
            ("Rowing Machine", Equipment.Machine),
            ("Stair Climber", Equipment.Machine),
            ("StairMaster", Equipment.Machine),
            ("Assault Bike", Equipment.Machine),
            ("Ski Erg", Equipment.Machine),
            ("Jump Rope", Equipment.Other),
            ("Double Unders", Equipment.Other),
            ("Burpees", Equipment.Bodyweight),
            ("High Knees", Equipment.Bodyweight),
            ("Jumping Jacks", Equipment.Bodyweight),
            ("Sprints", Equipment.None),
            ("Outdoor Running", Equipment.None),
            ("Outdoor Cycling", Equipment.None),
            ("Swimming", Equipment.None),
            ("Battle Ropes", Equipment.Other),
        }));

        // ─── FLEXIBILITY / STRETCHING ──────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.FullBody, ExerciseCategory.Flexibility, new[]
        {
            ("Hamstring Stretch", Equipment.None),
            ("Quad Stretch", Equipment.None),
            ("Hip Flexor Stretch", Equipment.None),
            ("Chest Stretch (Doorway)", Equipment.None),
            ("Shoulder Stretch (Cross-Body)", Equipment.None),
            ("Tricep Stretch (Overhead)", Equipment.None),
            ("Lat Stretch", Equipment.None),
            ("Cat-Cow Stretch", Equipment.None),
            ("Child's Pose", Equipment.None),
            ("Pigeon Pose", Equipment.None),
            ("Seated Forward Fold", Equipment.None),
            ("Standing Calf Stretch", Equipment.None),
            ("Foam Rolling (Full Body)", Equipment.Other),
            ("Foam Rolling (IT Band)", Equipment.Other),
            ("Foam Rolling (Back)", Equipment.Other),
        }));

        // ─── PLYOMETRICS ──────────────────────────────────────────────────
        exercises.AddRange(CreateExercises(ref index, MuscleGroup.FullBody, ExerciseCategory.Plyometric, new[]
        {
            ("Box Jump", Equipment.Other),
            ("Depth Jump", Equipment.Other),
            ("Broad Jump", Equipment.Bodyweight),
            ("Tuck Jump", Equipment.Bodyweight),
            ("Lateral Bound", Equipment.Bodyweight),
            ("Plyo Push-Up", Equipment.Bodyweight),
            ("Medicine Ball Slam", Equipment.Other),
            ("Medicine Ball Chest Pass", Equipment.Other),
            ("Medicine Ball Rotational Throw", Equipment.Other),
            ("Skater Jump", Equipment.Bodyweight),
        }));

        return exercises.ToArray();
    }

    private static List<object> CreateExercises(
        ref int index,
        MuscleGroup muscleGroup,
        ExerciseCategory category,
        (string Name, Equipment Equipment)[] items)
    {
        var result = new List<object>();
        foreach (var (name, equipment) in items)
        {
            index++;
            result.Add(new
            {
                Id = CreateDeterministicGuid(index),
                Name = name,
                Description = (string?)null,
                Instructions = (string?)null,
                DemoMediaUrl = (string?)null,
                MuscleGroups = new List<MuscleGroup> { muscleGroup },
                Equipment = equipment,
                Category = category,
                Scope = ExerciseScope.Global,
                GymId = (Guid?)null,
                CreatedBy = (Guid?)null,
                IsActive = true
            });
        }
        return result;
    }

    private static Guid CreateDeterministicGuid(int index)
    {
        // Deterministic GUID based on namespace + index
        var bytes = Namespace.ToByteArray();
        var indexBytes = BitConverter.GetBytes(index);
        for (int i = 0; i < 4; i++)
        {
            bytes[i] ^= indexBytes[i];
        }
        return new Guid(bytes);
    }
}
