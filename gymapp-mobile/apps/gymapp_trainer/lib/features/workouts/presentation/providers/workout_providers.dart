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
  return apiClient.get<List<ExerciseModel>>(
    ApiEndpoints.exercises,
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <ExerciseModel>[];
      }
      return list
          .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
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
