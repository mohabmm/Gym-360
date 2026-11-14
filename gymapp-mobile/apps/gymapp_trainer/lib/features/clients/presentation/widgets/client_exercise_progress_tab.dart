import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/router.dart';
import '../../../sessions/presentation/pt/exercises/exercise_progress_providers.dart';
import '../providers/client_providers.dart';

/// pt.exercise_progress: the client's board as they see it, plus a shortcut to record the
/// exercises of their latest completed session.
class ClientExerciseProgressTab extends ConsumerWidget {
  final String clientId;
  final List<SessionModel> sessions;
  final bool isArabic;

  const ClientExerciseProgressTab({super.key, required this.clientId, required this.sessions, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final goal = ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptProgressMeasurements)
        ? ref.watch(clientJourneyProvider(clientId)).valueOrNull?.goalType
        : null;
    return RefreshIndicator(
      onRefresh: () => ref.refresh(clientExerciseProgressProvider(clientId).future),
      child: ref.watch(clientExerciseProgressProvider(clientId)).when(
            data: (board) => ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _RecordLatest(clientId: clientId, session: latestCompleted(sessions), isArabic: isArabic),
                ExerciseProgressView(
                  board: board,
                  goal: goal,
                  isArabic: isArabic,
                  emptyBody: ExerciseProgressStrings(isArabic).coachEmptyBody,
                ),
              ],
            ),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (_, __) => AppErrorWidget(
              message: "Couldn't load this client's exercises",
              messageAr: 'تعذر تحميل تمارين العميل',
              onRetry: () => ref.invalidate(clientExerciseProgressProvider(clientId)),
            ),
          ),
    );
  }

  /// The newest completed session, the one a coach usually records right after training.
  static SessionModel? latestCompleted(List<SessionModel> sessions) {
    final done = sessions.where((s) => s.status.toLowerCase() == 'completed').toList()
      ..sort((a, b) => b.scheduledAt.compareTo(a.scheduledAt));
    return done.firstOrNull;
  }
}

class _RecordLatest extends StatelessWidget {
  final String clientId;
  final SessionModel? session;
  final bool isArabic;

  const _RecordLatest({required this.clientId, required this.session, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final latest = session;
    if (latest == null) return const SizedBox.shrink();
    final date = DateFormat('d MMM', isArabic ? 'ar' : 'en').format(latest.scheduledAt);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: FilledButton.tonalIcon(
        onPressed: () => context.push(AppRoutes.sessionExercisesFor(latest.id, clientId)),
        icon: const Icon(Icons.edit_note),
        label: Text(isArabic ? 'سجّل تمارين آخر حصة · $date' : 'Record last session\'s exercises · $date'),
      ),
    );
  }
}
