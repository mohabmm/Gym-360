import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../../core/providers.dart';
import 'session_exercises_draft.dart';

/// pt.exercise_progress: the client's board (own clients only; the API answers 403 otherwise).
final clientExerciseProgressProvider =
    FutureProvider.autoDispose.family<ExerciseProgressModel, String>((ref, clientId) async {
  return ref.watch(apiClientProvider).get<ExerciseProgressModel>(
        ApiEndpoints.clientExerciseProgress(ClientId(clientId)),
        parser: (data) => ExerciseProgressModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// Saves the session's exercises (and day label), then refreshes the client's board.
Future<void> saveSessionExercises(
  WidgetRef ref, {
  required String sessionId,
  required String clientId,
  required SessionExercisesDraft draft,
}) async {
  await ref.read(apiClientProvider).post<Object?>(
        ApiEndpoints.sessionExerciseLogs(SessionId(sessionId)),
        data: draft.toJson(),
        parser: (data) => data,
      );
  ref.invalidate(clientExerciseProgressProvider(clientId));
}
