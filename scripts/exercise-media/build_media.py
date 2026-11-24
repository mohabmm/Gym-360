"""Builds the exercise demo animations from free-exercise-db (public domain, The Unlicense).

For every global library exercise (ExerciseSeedData.cs) with a match in free-exercise-db, the
two photos (start and end position) become one looping animated WebP at
src/API/GymApp.Api/wwwroot/media/exercises/{slug}.webp, and exercise-media-map.json records
library name -> file. The API serves global exercises with that animation (ExerciseDemoMedia).

Matching = exact normalised name, then the reviewed OVERRIDES below. Names in UNMATCHED have no
honest picture in the source and keep no media (the apps show a placeholder).

Run (Pillow needed; e.g. in a python:3.12-slim container):
    python build_media.py <exercises.json> <images dir> <repo root>
The free-exercise-db checkout gives exercises.json (dist/) and the images dir (exercises/).
"""
import json
import re
import sys
from pathlib import Path

from PIL import Image

SIZE = 360          # longest side in px: sharp on a phone sheet, small to download
FRAME_MS = 900      # time on each position
QUALITY = 62

# Library name -> free-exercise-db id, reviewed by hand (same movement, close enough to teach).
OVERRIDES = {
    "Barbell Bench Press": "Barbell_Bench_Press_-_Medium_Grip",
    "Incline Barbell Bench Press": "Barbell_Incline_Bench_Press_-_Medium_Grip",
    "Close-Grip Bench Press": "Close-Grip_Barbell_Bench_Press",
    "Close-Grip Barbell Bench Press": "Close-Grip_Barbell_Bench_Press",
    "Incline Dumbbell Bench Press": "Incline_Dumbbell_Press",
    "Dumbbell Fly": "Dumbbell_Flyes",
    "Incline Dumbbell Fly": "Incline_Dumbbell_Flyes",
    "Decline Dumbbell Fly": "Decline_Dumbbell_Flyes",
    "Dumbbell Pullover": "Bent-Arm_Dumbbell_Pullover",
    "Cable Fly": "Cable_Crossover",
    "Machine Chest Press": "Machine_Bench_Press",
    "Machine Incline Chest Press": "Leverage_Incline_Chest_Press",
    "Pec Deck Machine": "Butterfly",
    "Push-Ups": "Pushups",
    "Wide Push-Ups": "Push-Up_Wide",
    "Diamond Push-Ups": "Push-Ups_-_Close_Triceps_Position",
    "Diamond Push-Ups (Triceps)": "Push-Ups_-_Close_Triceps_Position",
    "Plyo Push-Up": "Plyo_Push-up",
    "Chest Dips": "Dips_-_Chest_Version",
    "Conventional Deadlift": "Barbell_Deadlift",
    "T-Bar Row": "Lying_T-Bar_Row",
    "Dumbbell Row": "One-Arm_Dumbbell_Row",
    "Dumbbell Deadlift": "Stiff-Legged_Dumbbell_Deadlift",
    "Lat Pulldown": "Wide-Grip_Lat_Pulldown",
    "Machine Lat Pulldown": "Full_Range-Of-Motion_Lat_Pulldown",
    "Close-Grip Lat Pulldown": "Close-Grip_Front_Lat_Pulldown",
    "Seated Cable Row": "Seated_Cable_Rows",
    "Single-Arm Cable Row": "Seated_One-arm_Cable_Pulley_Rows",
    "Pull-Ups": "Pullups",
    "Chin-Ups": "Chin-Up",
    "Neutral-Grip Pull-Ups": "V-Bar_Pullup",
    "Back Extension": "Hyperextensions_Back_Extensions",
    "Hyperextension": "Hyperextensions_Back_Extensions",
    "Kettlebell Swing": "One-Arm_Kettlebell_Swings",
    "Overhead Press (Military Press)": "Standing_Military_Press",
    "Seated Barbell Press": "Seated_Barbell_Military_Press",
    "Arnold Press": "Arnold_Dumbbell_Press",
    "Seated Dumbbell Press": "Seated_Dumbbell_Press",
    "Dumbbell Lateral Raise": "Side_Lateral_Raise",
    "Dumbbell Front Raise": "Front_Dumbbell_Raise",
    "Dumbbell Rear Delt Fly": "Seated_Bent-Over_Rear_Delt_Raise",
    "Dumbbell Upright Row": "Standing_Dumbbell_Upright_Row",
    "Barbell Upright Row": "Upright_Barbell_Row",
    "Cable Lateral Raise": "Cable_Seated_Lateral_Raise",
    "Cable Front Raise": "Front_Cable_Raise",
    "Cable Face Pull": "Face_Pull",
    "Cable Rear Delt Fly": "Cable_Rear_Delt_Fly",
    "Machine Shoulder Press": "Machine_Shoulder_Military_Press",
    "Machine Rear Delt Fly": "Reverse_Machine_Flyes",
    "Smith Machine Shoulder Press": "Smith_Machine_Overhead_Shoulder_Press",
    "Kettlebell Press": "Two-Arm_Kettlebell_Military_Press",
    "Plate Front Raise": "Front_Plate_Raise",
    "Preacher Curl (Barbell)": "Preacher_Curl",
    "Dumbbell Curl": "Dumbbell_Bicep_Curl",
    "Hammer Curl": "Hammer_Curls",
    "Incline Dumbbell Curl": "Incline_Dumbbell_Curl",
    "Concentration Curl": "Concentration_Curls",
    "Cross-Body Hammer Curl": "Cross_Body_Hammer_Curl",
    "Preacher Curl (Dumbbell)": "One_Arm_Dumbbell_Preacher_Curl",
    "Cable Curl": "Standing_Biceps_Cable_Curl",
    "Cable Hammer Curl (Rope)": "Cable_Hammer_Curls_-_Rope_Attachment",
    "High Cable Curl": "High_Cable_Curls",
    "Machine Preacher Curl": "Machine_Preacher_Curls",
    "Machine Bicep Curl": "Machine_Bicep_Curl",
    "EZ-Bar Curl": "EZ-Bar_Curl",
    "Tricep Pushdown (Cable)": "Triceps_Pushdown",
    "Tricep Rope Pushdown": "Triceps_Pushdown_-_Rope_Attachment",
    "Overhead Cable Tricep Extension": "Cable_Rope_Overhead_Triceps_Extension",
    "Skull Crusher (EZ-Bar)": "EZ-Bar_Skullcrusher",
    "Barbell Overhead Tricep Extension": "Standing_Overhead_Barbell_Triceps_Extension",
    "Dumbbell Overhead Tricep Extension": "Standing_Dumbbell_Triceps_Extension",
    "Dumbbell Kickback": "Tricep_Dumbbell_Kickback",
    "Tricep Dips": "Dips_-_Triceps_Version",
    "Machine Dip": "Dip_Machine",
    "Barbell Back Squat": "Barbell_Squat",
    "Dumbbell Lunge": "Dumbbell_Lunges",
    "Dumbbell Walking Lunge": "Bodyweight_Walking_Lunge",
    "Dumbbell Step-Up": "Dumbbell_Step_Ups",
    "Barbell Step-Up": "Barbell_Step_Ups",
    "Dumbbell Bulgarian Split Squat": "Split_Squat_with_Dumbbells",
    "Hack Squat Machine": "Hack_Squat",
    "Leg Extension Machine": "Leg_Extensions",
    "Sissy Squat": "Weighted_Sissy_Squat",
    "Pistol Squat": "Kettlebell_Pistol_Squat",
    "Jump Squat": "Freehand_Jump_Squat",
    "Box Jump": "Front_Box_Jump",
    "Lying Leg Curl Machine": "Lying_Leg_Curls",
    "Seated Leg Curl Machine": "Seated_Leg_Curl",
    "Standing Leg Curl Machine": "Standing_Leg_Curl",
    "Stiff-Leg Deadlift": "Stiff-Legged_Barbell_Deadlift",
    "Dumbbell Romanian Deadlift": "Stiff-Legged_Dumbbell_Deadlift",
    "Glute-Ham Raise": "Glute_Ham_Raise",
    "Nordic Hamstring Curl": "Natural_Glute_Ham_Raise",
    "Cable Pull-Through": "Pull_Through",
    "Standing Calf Raise (Machine)": "Standing_Calf_Raises",
    "Seated Calf Raise (Machine)": "Seated_Calf_Raise",
    "Leg Press Calf Raise": "Calf_Press_On_The_Leg_Press_Machine",
    "Donkey Calf Raise": "Donkey_Calf_Raises",
    "Barbell Calf Raise": "Standing_Barbell_Calf_Raise",
    "Dumbbell Calf Raise": "Standing_Dumbbell_Calf_Raise",
    "Bodyweight Calf Raise": "Standing_Calf_Raises",
    "Glute Bridge": "Butt_Lift_Bridge",
    "Single-Leg Glute Bridge": "Single_Leg_Glute_Bridge",
    "Cable Kickback": "One-Legged_Cable_Kickback",
    "Cable Hip Abduction": "Thigh_Abductor",
    "Machine Hip Abduction": "Thigh_Abductor",
    "Machine Hip Adduction": "Thigh_Adductor",
    "Banded Lateral Walk": "Monster_Walk",
    "Sumo Squat": "Plie_Dumbbell_Squat",
    "Bicycle Crunches": "Air_Bike",
    "Reverse Crunches": "Reverse_Crunch",
    "Sit-Ups": "Sit-Up",
    "Leg Raises (Lying)": "Flat_Bench_Lying_Leg_Raise",
    "Hanging Leg Raises": "Hanging_Leg_Raise",
    "Hanging Knee Raises": "Hanging_Leg_Raise",
    "Ab Wheel Rollout": "Ab_Roller",
    "Cable Pallof Press": "Pallof_Press",
    "Machine Crunch": "Ab_Crunch_Machine",
    "Decline Sit-Ups": "Decline_Crunch",
    "Toe Touches": "Toe_Touchers",
    "Side Plank": "Push_Up_to_Side_Plank",
    "Treadmill Running": "Running_Treadmill",
    "Treadmill Walking": "Walking_Treadmill",
    "Treadmill Incline Walking": "Walking_Treadmill",
    "Stationary Bike": "Bicycling_Stationary",
    "Recumbent Bike": "Recumbent_Bike",
    "Rowing Machine": "Rowing_Stationary",
    "Stair Climber": "Stairmaster",
    "StairMaster": "Stairmaster",
    "Jump Rope": "Rope_Jumping",
    "Sprints": "Wind_Sprints",
    "Battle Ropes": "Battling_Ropes",
    "Hip Flexor Stretch": "Kneeling_Hip_Flexor",
    "Chest Stretch (Doorway)": "Chest_And_Front_Of_Shoulder_Stretch",
    "Shoulder Stretch (Cross-Body)": "Shoulder_Stretch",
    "Tricep Stretch (Overhead)": "Tricep_Side_Stretch",
    "Cat-Cow Stretch": "Cat_Stretch",
    "Child's Pose": "Childs_Pose",
    "Standing Calf Stretch": "Calf_Stretch_Hands_Against_Wall",
    "Seated Forward Fold": "Seated_Floor_Hamstring_Stretch",
    "Foam Rolling (IT Band)": "Iliotibial_Tract-SMR",
    "Foam Rolling (Back)": "Lower_Back-SMR",
    "Depth Jump": "Depth_Jump_Leap",
    "Tuck Jump": "Knee_Tuck_Jump",
    "Lateral Bound": "Lateral_Bound",
    "Medicine Ball Chest Pass": "Medicine_Ball_Chest_Pass",
    "Broad Jump": "Standing_Long_Jump",
    "Medicine Ball Slam": "Overhead_Slam",
    "Barbell Front Squat": "Front_Squat_Clean_Grip",
    "Zercher Squat": "Zercher_Squats",
}

# No honest match in the source: no media rather than a misleading one.
UNMATCHED = {
    "Pendlay Row", "Reverse-Grip Lat Pulldown", "Chest-Supported Row Machine", "Machine Row",
    "Straight-Arm Pulldown", "Smith Machine Bent-Over Row", "Landmine Press", "Pike Push-Ups",
    "Machine Lateral Raise", "21s Curl", "Kettlebell Curl", "Single-Arm Cable Pushdown",
    "Dumbbell Skull Crusher", "V-Squat Machine", "Pendulum Squat", "Wall Sit",
    "Kettlebell Goblet Squat", "Single-Leg Romanian Deadlift", "Single-Leg Calf Raise",
    "Dumbbell Hip Thrust", "Machine Hip Thrust", "Banded Clamshell", "Kettlebell Sumo Deadlift",
    "Frog Pump", "V-Ups", "Bird Dog", "Cable Woodchop", "Dragon Flag", "Hollow Body Hold",
    "Assault Bike", "Ski Erg", "Double Unders", "Burpees", "High Knees", "Jumping Jacks",
    "Outdoor Running", "Outdoor Cycling", "Swimming", "Lat Stretch", "Pigeon Pose",
    "Foam Rolling (Full Body)", "Skater Jump", "Svend Press", "JM Press", "Tate Press",
    "Medicine Ball Rotational Throw", "Kettlebell Tricep Extension",
}


def norm(name: str) -> str:
    words = re.findall(r"[a-z0-9]+", name.lower().replace("ups", "up"))
    return " ".join(sorted(w.rstrip("s") for w in words))


def slug(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")


def library_names(repo: Path) -> list[str]:
    seed = repo / "src/Modules/Training/GymApp.Modules.Training.Infrastructure/Seeds/ExerciseSeedData.cs"
    return list(dict.fromkeys(re.findall(r'^\s+\("([^"]+)"', seed.read_text(encoding="utf-8"), re.M)))


def match(names: list[str], free: list[dict]) -> dict[str, dict]:
    by_id = {e["id"]: e for e in free}
    by_norm = {norm(e["name"]): e for e in free}
    found = {}
    for name in names:
        if name in UNMATCHED:
            continue
        entry = by_id.get(OVERRIDES[name]) if name in OVERRIDES else by_norm.get(norm(name))
        if entry is None or len(entry.get("images", [])) < 2:
            raise SystemExit(f"No two-photo match for '{name}': add it to OVERRIDES or UNMATCHED")
        found[name] = entry
    return found


def frame(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGB")
    image.thumbnail((SIZE, SIZE))
    return image


def same_size(frames: list[Image.Image]) -> list[Image.Image]:
    """Centres every frame on one white canvas: an animation needs equal frame sizes."""
    width, height = max(f.width for f in frames), max(f.height for f in frames)
    canvases = []
    for f in frames:
        canvas = Image.new("RGB", (width, height), "white")
        canvas.paste(f, ((width - f.width) // 2, (height - f.height) // 2))
        canvases.append(canvas)
    return canvases


def build(entry: dict, images: Path, target: Path) -> None:
    frames = same_size([frame(images / p) for p in entry["images"][:2]])
    frames[0].save(target, save_all=True, append_images=frames[1:], duration=FRAME_MS, loop=0, quality=QUALITY, method=6)


def main() -> None:
    free = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    images, repo = Path(sys.argv[2]), Path(sys.argv[3])
    out = repo / "src/API/GymApp.Api/wwwroot/media/exercises"
    out.mkdir(parents=True, exist_ok=True)
    found = match(library_names(repo), free)
    mapping = {}
    for name, entry in sorted(found.items()):
        file = f"{slug(name)}.webp"
        build(entry, images, out / file)
        mapping[name] = {"file": file, "source": entry["id"]}
    map_path = repo / "src/Modules/Training/GymApp.Modules.Training.Application/Exercises/exercise-media-map.json"
    map_path.write_text(json.dumps(mapping, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{len(mapping)} animations, {len(UNMATCHED)} without media")


if __name__ == "__main__":
    main()
