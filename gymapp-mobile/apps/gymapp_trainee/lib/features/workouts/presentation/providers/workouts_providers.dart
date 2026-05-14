import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/workouts_remote_datasource.dart';
import '../../data/repositories/workouts_repository.dart';

final workoutsRemoteDataSourceProvider =
    Provider<WorkoutsRemoteDataSource>((ref) {
  return WorkoutsRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final workoutsRepositoryProvider = Provider<WorkoutsRepository>((ref) {
  return WorkoutsRepository(
    remoteDataSource: ref.watch(workoutsRemoteDataSourceProvider),
  );
});

/// All workouts (assigned + templates)
final workoutsProvider = FutureProvider<List<WorkoutModel>>((ref) async {
  final repo = ref.watch(workoutsRepositoryProvider);
  return repo.getWorkouts();
});

/// Workout assignments from trainer
final workoutAssignmentsProvider =
    FutureProvider<List<WorkoutAssignmentModel>>((ref) async {
  final repo = ref.watch(workoutsRepositoryProvider);
  return repo.getWorkoutAssignments();
});

/// Single workout detail
final workoutDetailProvider =
    FutureProvider.family<WorkoutModel, String>((ref, id) async {
  final repo = ref.watch(workoutsRepositoryProvider);
  return repo.getWorkoutById(id);
});

/// Workout history/logs
final workoutLogsProvider =
    FutureProvider<List<WorkoutLogModel>>((ref) async {
  final repo = ref.watch(workoutsRepositoryProvider);
  return repo.getWorkoutLogs();
});

/// Exercise library with optional filter
final exerciseSearchQueryProvider = StateProvider<String>((ref) => '');
final exerciseMuscleGroupFilterProvider = StateProvider<String?>((ref) => null);

final exerciseLibraryProvider =
    FutureProvider<List<ExerciseModel>>((ref) async {
  final query = ref.watch(exerciseSearchQueryProvider);
  final muscleGroup = ref.watch(exerciseMuscleGroupFilterProvider);
  final repo = ref.watch(workoutsRepositoryProvider);
  return repo.getExercises(
    query: query.isEmpty ? null : query,
    muscleGroup: muscleGroup,
  );
});

/// Active guided workout state
class GuidedWorkoutState {
  final WorkoutModel workout;
  final int currentExerciseIndex;
  final int currentSetIndex;
  final List<LogSetModel> completedSets;
  final bool isComplete;
  final DateTime startTime;

  const GuidedWorkoutState({
    required this.workout,
    this.currentExerciseIndex = 0,
    this.currentSetIndex = 0,
    this.completedSets = const [],
    this.isComplete = false,
    required this.startTime,
  });

  GuidedWorkoutState copyWith({
    int? currentExerciseIndex,
    int? currentSetIndex,
    List<LogSetModel>? completedSets,
    bool? isComplete,
  }) {
    return GuidedWorkoutState(
      workout: workout,
      currentExerciseIndex: currentExerciseIndex ?? this.currentExerciseIndex,
      currentSetIndex: currentSetIndex ?? this.currentSetIndex,
      completedSets: completedSets ?? this.completedSets,
      isComplete: isComplete ?? this.isComplete,
      startTime: startTime,
    );
  }
}

class GuidedWorkoutNotifier extends Notifier<GuidedWorkoutState?> {
  @override
  GuidedWorkoutState? build() => null;

  void startWorkout(WorkoutModel workout) {
    state = GuidedWorkoutState(
      workout: workout,
      startTime: DateTime.now(),
    );
  }

  void completeSet(LogSetModel setLog) {
    final current = state;
    if (current == null) return;

    final newSets = [...current.completedSets, setLog];
    final exercises = current.workout.exercises;
    final exercise = exercises[current.currentExerciseIndex];

    int nextSet = current.currentSetIndex + 1;
    int nextExercise = current.currentExerciseIndex;

    if (nextSet >= exercise.sets) {
      nextSet = 0;
      nextExercise++;
    }

    final isComplete = nextExercise >= exercises.length;

    state = current.copyWith(
      currentExerciseIndex: nextExercise,
      currentSetIndex: nextSet,
      completedSets: newSets,
      isComplete: isComplete,
    );
  }

  void skipExercise() {
    final current = state;
    if (current == null) return;

    final nextExercise = current.currentExerciseIndex + 1;
    final isComplete = nextExercise >= current.workout.exercises.length;

    state = current.copyWith(
      currentExerciseIndex: nextExercise,
      currentSetIndex: 0,
      isComplete: isComplete,
    );
  }

  void endWorkout() {
    state = null;
  }

  Future<void> finishAndLog(WidgetRef ref) async {
    final current = state;
    if (current == null) return;

    final now = DateTime.now();
    final duration = now.difference(current.startTime).inMinutes;
    final avgRpe = _averageRpe(current.completedSets);

    final repo = ref.read(workoutsRepositoryProvider);
    try {
      await repo.logWorkout({
        if (current.workout.id.isNotEmpty) 'workoutId': current.workout.id,
        'date': _formatDate(now),
        'startedAt': current.startTime.toUtc().toIso8601String(),
        'completedAt': now.toUtc().toIso8601String(),
        if (duration > 0) 'durationMinutes': duration,
        'sets': current.completedSets.map(_setToApiDto).toList(),
        if (avgRpe != null) 'perceivedExertion': avgRpe,
      });
      ref.invalidate(workoutLogsProvider);
      ref.invalidate(workoutsProvider);
    } finally {
      state = null;
    }
  }
}

String _formatDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-'
    '${d.month.toString().padLeft(2, '0')}-'
    '${d.day.toString().padLeft(2, '0')}';

Map<String, dynamic> _setToApiDto(LogSetModel s) => {
      'exerciseId': s.exerciseId,
      'setNumber': s.setNumber,
      'reps': s.reps ?? 0,
      if (s.weight != null) 'weight': s.weight,
      'weightUnit': 'kg',
      if (s.durationSeconds != null) 'durationSeconds': s.durationSeconds,
      'isCompleted': true,
      if (s.notes != null && s.notes!.isNotEmpty) 'notes': s.notes,
    };

int? _averageRpe(List<LogSetModel> sets) {
  final values = sets.map((s) => s.rpe).whereType<int>().toList();
  if (values.isEmpty) return null;
  final avg = values.reduce((a, b) => a + b) / values.length;
  return avg.round().clamp(1, 10);
}

final guidedWorkoutProvider =
    NotifierProvider<GuidedWorkoutNotifier, GuidedWorkoutState?>(
  GuidedWorkoutNotifier.new,
);
