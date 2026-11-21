import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';

import '../../../scheduling/presentation/widgets/pt_conversion_widgets.dart';
import '../providers/exercise_progress_providers.dart';
import '../providers/progress_providers.dart';

/// pt.exercise_progress: "My training" — what the coach recorded, session by session (default)
/// or exercise by exercise, read against the member's goal. During the free trial it holds the
/// trial sessions and leads with the "keep training" card once they are used; after paying it
/// keeps every session.
class ExerciseProgressScreen extends ConsumerWidget {
  const ExerciseProgressScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final strings = ExerciseProgressStrings(isArabic);
    final goal = ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptProgressMeasurements)
        ? ref.watch(progressJourneyProvider).valueOrNull?.goalType
        : null;

    return RtlScaffold(
      isArabic: isArabic,
      appBar: AppBar(title: Text(strings.title)),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(myExerciseProgressProvider.future),
        child: ref.watch(myExerciseProgressProvider).when(
              data: (board) => ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  ExerciseProgressView(
                    board: board,
                    goal: goal,
                    isArabic: isArabic,
                    header: board.summary.isTrialView ? _ConversionHeader(board: board, strings: strings) : null,
                    footer: board.summary.isTrialView ? _TrialFooter(strings: strings) : null,
                    onOpenSession: (id) => context.push(AppRoutes.sessionDetail.replaceFirst(':sessionId', id)),
                  ),
                ],
              ),
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, __) => AppErrorWidget(
                message: "Couldn't load your exercise progress",
                messageAr: 'تعذر تحميل تقدم التمارين',
                onRetry: () => ref.invalidate(myExerciseProgressProvider),
              ),
            ),
      ),
    );
  }
}

/// Trial over (or the coach sent a plan): the conversion card leads the screen, with what the
/// member keeps by continuing. Renders nothing while the trial is still running.
class _ConversionHeader extends ConsumerWidget {
  final ExerciseProgressModel board;
  final ExerciseProgressStrings strings;

  const _ConversionHeader({required this.board, required this.strings});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final finished = ref.watch(finishedTrialProvider) != null;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const TrialConversionCard(),
          if (finished) ...[
            const SizedBox(height: 6),
            Text(strings.keepProgress(board), style: Theme.of(context).textTheme.bodySmall, textAlign: TextAlign.center),
          ],
        ],
      ),
    );
  }
}

/// Trial still running: a quiet line on why tracking matters.
class _TrialFooter extends StatelessWidget {
  final ExerciseProgressStrings strings;

  const _TrialFooter({required this.strings});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 8),
        child: Text(strings.keepTracking, style: Theme.of(context).textTheme.bodySmall, textAlign: TextAlign.center),
      );
}
