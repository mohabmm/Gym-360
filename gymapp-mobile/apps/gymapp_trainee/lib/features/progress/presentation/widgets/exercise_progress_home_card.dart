import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/exercise_progress_providers.dart';
import '../providers/progress_providers.dart';

/// Home: the board's one-line story ("Stronger on 3 of 4 exercises"). Nothing until the coach logs exercises.
class ExerciseProgressHomeCard extends ConsumerWidget {
  const ExerciseProgressHomeCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final board = ref.watch(myExerciseProgressProvider).valueOrNull;
    if (board == null || board.isEmpty) return const SizedBox.shrink();

    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final strings = ExerciseProgressStrings(isArabic);
    final goal = ref.watch(progressJourneyProvider).valueOrNull?.goalType;
    final insight = BoardInsight.of(board, goal);
    final text = Theme.of(context).textTheme;
    return AppCard(
      onTap: () => context.push(AppRoutes.exerciseProgress),
      padding: const EdgeInsets.all(16),
      child: Row(
        children: [
          const Icon(Icons.fitness_center, color: AppColors.primary),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(strings.title, style: text.labelMedium?.copyWith(color: AppColors.grey500)),
                const SizedBox(height: 2),
                Text(strings.headline(insight), style: text.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
                const SizedBox(height: 2),
                Text(strings.trialProgress(board.summary), style: text.bodySmall),
              ],
            ),
          ),
          Icon(isArabic ? Icons.chevron_left : Icons.chevron_right, color: AppColors.grey400),
        ],
      ),
    );
  }
}
