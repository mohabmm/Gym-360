import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';

/// Color for a change: green = progress for the goal, red = away from it, grey = no judgement.
Color toneColor(ChangeTone tone) => switch (tone) {
      ChangeTone.good => AppColors.success,
      ChangeTone.bad => AppColors.error,
      ChangeTone.neutral => AppColors.grey500,
    };

/// "↓ 4.2 kg" pill colored by goal.
class ChangeChip extends StatelessWidget {
  final MetricValue change;
  final ChangeTone tone;
  final ProgressJourneyStrings strings;
  final bool large;

  const ChangeChip({
    super.key,
    required this.change,
    required this.tone,
    required this.strings,
    this.large = false,
  });

  @override
  Widget build(BuildContext context) {
    final color = toneColor(tone);
    final text = Theme.of(context).textTheme;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: large ? 12 : 8, vertical: large ? 6 : 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        strings.delta(change),
        style: (large ? text.titleSmall : text.labelSmall)?.copyWith(color: color, fontWeight: FontWeight.w700),
      ),
    );
  }
}
