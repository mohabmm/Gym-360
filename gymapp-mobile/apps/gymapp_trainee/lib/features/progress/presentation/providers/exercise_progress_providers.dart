import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// pt.exercise_progress: every number the coach recorded for the member (gym from the JWT).
final myExerciseProgressProvider = FutureProvider.autoDispose<ExerciseProgressModel>((ref) async {
  return ref.watch(apiClientProvider).get<ExerciseProgressModel>(
        ApiEndpoints.myExerciseProgress,
        parser: (data) => ExerciseProgressModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});
