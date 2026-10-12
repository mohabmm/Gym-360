import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// All workouts created by this trainer.
final trainerWorkoutsProvider =
    FutureProvider.autoDispose<List<WorkoutModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  final gymId = ref.watch(activeGymIdProvider);
  if (trainerId == null || gymId == null) return <WorkoutModel>[];
  return apiClient.get<List<WorkoutModel>>(
    ApiEndpoints.workouts,
    queryParameters: {'trainerId': trainerId, 'gymId': gymId},
    parser: (data) {
      if (data is List) {
        return data
            .map((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return <WorkoutModel>[];
    },
  );
});

/// Workout templates.
final workoutTemplatesProvider =
    FutureProvider.autoDispose<List<WorkoutModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (gymId == null) return <WorkoutModel>[];
  return apiClient.get<List<WorkoutModel>>(
    ApiEndpoints.workoutTemplates,
    queryParameters: {'gymId': gymId},
    parser: (data) {
      if (data is List) {
        return data
            .map((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return <WorkoutModel>[];
    },
  );
});

/// A single workout by ID.
final workoutDetailProvider =
    FutureProvider.autoDispose.family<WorkoutModel, String>((ref, id) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<WorkoutModel>(
    ApiEndpoints.workoutById(id),
    parser: (data) => WorkoutModel.fromJson(data as Map<String, dynamic>),
  );
});

/// All exercises (library).
final exerciseLibraryProvider =
    FutureProvider.autoDispose<List<ExerciseModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);

  // The library is searched client-side, so every page has to be loaded:
  // fetching only page 1 left everything past the first 100 names
  // (alphabetically) impossible to find or add to a workout.
  const pageSize = 100;
  const maxPages = 20; // safety net against a misbehaving totalPages
  final all = <ExerciseModel>[];
  for (var page = 1; page <= maxPages; page++) {
    final result = await apiClient.get<({List<ExerciseModel> items, int totalPages})>(
      ApiEndpoints.exercises,
      queryParameters: {'page': page, 'pageSize': pageSize},
      parser: (data) {
        final List<dynamic> list;
        var totalPages = 1;
        if (data is List) {
          list = data;
        } else if (data is Map<String, dynamic> && data['items'] is List) {
          list = data['items'] as List<dynamic>;
          totalPages = (data['totalPages'] as num?)?.toInt() ?? 1;
        } else {
          list = const [];
        }
        return (
          items: list
              .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
              .toList(),
          totalPages: totalPages,
        );
      },
    );
    all.addAll(result.items);
    if (result.items.isEmpty || page >= result.totalPages) break;
  }
  return all;
});

/// Custom exercises created by this trainer.
final customExercisesProvider =
    FutureProvider.autoDispose<List<ExerciseModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<ExerciseModel>>(
    ApiEndpoints.customExercises,
    parser: (data) {
      if (data is List) {
        return data
            .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return <ExerciseModel>[];
    },
  );
});

/// All programs created by this trainer.
final trainerProgramsProvider =
    FutureProvider.autoDispose<List<ProgramModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  final gymId = ref.watch(activeGymIdProvider);
  if (trainerId == null || gymId == null) return <ProgramModel>[];
  return apiClient.get<List<ProgramModel>>(
    ApiEndpoints.programs,
    queryParameters: {'trainerId': trainerId, 'gymId': gymId},
    parser: (data) {
      if (data is List) {
        return data
            .map((e) => ProgramModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return <ProgramModel>[];
    },
  );
});
