# Exercise demo images

`src/API/GymApp.Api/wwwroot/media/exercises/*.webp` are built by `build_media.py` from the
photos of [free-exercise-db](https://github.com/yuhonas/free-exercise-db), released into the
public domain under [The Unlicense](https://github.com/yuhonas/free-exercise-db/blob/main/LICENSE).
No attribution is required; this note records where they come from.

To rebuild after changing the library or the mapping: download `dist/exercises.json` and the
needed `exercises/<id>/0.jpg, 1.jpg` photos, then run the script with Pillow
(`python build_media.py exercises.json <photos dir> <repo root>`).
