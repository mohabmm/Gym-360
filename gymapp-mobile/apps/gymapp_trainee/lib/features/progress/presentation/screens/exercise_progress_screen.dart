import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../scheduling/presentation/widgets/pt_conversion_widgets.dart';
import '../providers/exercise_progress_providers.dart';
import '../providers/progress_providers.dart';

/// pt.exercise_progress: the member's board — what the coach recorded per day and per
/// exercise, read against the member's goal. During the free trial it holds the trial
/// sessions; after paying it keeps every session.
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
                    footer: board.summary.isTrialView ? _TrialFooter(strings: strings) : null,
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

/// Trial view: why paying matters for the board, plus the conversion card once the trial ends.
class _TrialFooter extends StatelessWidget {
  final ExerciseProgressStrings strings;

  const _TrialFooter({required this.strings});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(strings.keepTracking, style: Theme.of(context).textTheme.bodySmall, textAlign: TextAlign.center),
            const SizedBox(height: 8),
            const TrialConversionCard(),
          ],
        ),
      );
}
