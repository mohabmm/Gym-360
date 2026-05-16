export type MuscleGroup =
  | 'chest' | 'back' | 'shoulders' | 'biceps' | 'triceps'
  | 'forearms' | 'abs' | 'obliques' | 'quads' | 'hamstrings'
  | 'glutes' | 'calves' | 'traps' | 'lats' | 'full_body' | 'cardio';

export type Equipment =
  | 'barbell' | 'dumbbell' | 'cable' | 'machine' | 'bodyweight'
  | 'kettlebell' | 'resistance_band' | 'smith_machine' | 'ez_bar'
  | 'trx' | 'medicine_ball' | 'pullup_bar' | 'bench' | 'none';

export type ExerciseCategory = 'strength' | 'cardio' | 'flexibility' | 'balance' | 'plyometric';

export type DifficultyLevel = 'beginner' | 'intermediate' | 'advanced';

export interface Exercise {
  id: string;
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  muscleGroup: MuscleGroup;
  secondaryMuscles: MuscleGroup[];
  equipment: Equipment;
  category: ExerciseCategory;
  difficulty: DifficultyLevel;
  demoVideoUrl?: string;
  thumbnailUrl?: string;
  instructions?: string;
  instructionsAr?: string;
  isCustom: boolean;
  gymId?: string;
  createdAt: string;
}

export interface WorkoutExercise {
  exerciseId: string;
  exerciseName: string;
  sets: number;
  reps?: number;
  duration?: number;
  restSeconds: number;
  weight?: number;
  notes?: string;
  order: number;
}

export interface Workout {
  id: string;
  name: string;
  nameAr: string;
  description?: string;
  trainerId: string;
  trainerName: string;
  exercises: WorkoutExercise[];
  estimatedDuration: number;
  difficulty: DifficultyLevel;
  createdAt: string;
}

export interface Program {
  id: string;
  name: string;
  nameAr: string;
  description?: string;
  trainerId: string;
  durationWeeks: number;
  workoutsPerWeek: number;
  difficulty: DifficultyLevel;
  workouts: Workout[];
  createdAt: string;
}

export interface WorkoutLog {
  id: string;
  workoutId: string;
  workoutName: string;
  memberId: string;
  completedAt: string;
  duration: number;
  exercises: WorkoutLogExercise[];
  notes?: string;
}

export interface WorkoutLogExercise {
  exerciseId: string;
  exerciseName: string;
  sets: WorkoutLogSet[];
}

export interface WorkoutLogSet {
  setNumber: number;
  reps: number;
  weight?: number;
  duration?: number;
  isCompleted: boolean;
}
