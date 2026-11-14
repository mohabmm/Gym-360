import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';

/// Check-in rhythm: every [cadenceDays] days. Due/overdue → a highlighted card with "Log now";
/// otherwise a slim countdown bar. Renders nothing before the first check-in.
class NextCheckInCard extends StatelessWidget {
  static const int cadenceDays = 14;

  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;
  final VoidCallback onLog;

  const NextCheckInCard({
    super.key,
    required this.journey,
    required this.strings,
    required this.now,
    required this.onLog,
  });

  int? get _daysSinceLast {
    final last = journey.latest;
    if (last == null) return null;
    final d = ProgressJourney.dateOf(last);
    return DateTime(now.year, now.month, now.day).difference(DateTime(d.year, d.month, d.day)).inDays;
  }

  @override
  Widget build(BuildContext context) {
    final since = _daysSinceLast;
    if (since == null) return const SizedBox.shrink();
    final remaining = cadenceDays - since;
    return remaining <= 0 ? _due(context) : _countdown(context, remaining);
  }

  Widget _due(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      key: const ValueKey('check-in-due'),
      padding: const EdgeInsets.all(16),
      color: theme.colorScheme.primary.withValues(alpha: 0.08),
      child: Row(children: [
        Icon(Icons.notifications_active_outlined, color: theme.colorScheme.primary, size: 28),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(strings.checkInDue, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 2),
            Text(strings.checkInDueBody, style: theme.textTheme.bodySmall),
          ]),
        ),
        const SizedBox(width: 8),
        FilledButton(onPressed: onLog, child: Text(strings.logNow)),
      ]),
    );
  }

  Widget _countdown(BuildContext context, int remaining) {
    final theme = Theme.of(context);
    return AppCard(
      key: const ValueKey('check-in-countdown'),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          const Icon(Icons.schedule, size: 18, color: AppColors.grey500),
          const SizedBox(width: 8),
          Expanded(child: Text(strings.nextCheckInIn(remaining), style: theme.textTheme.bodyMedium)),
        ]),
        const SizedBox(height: 8),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(value: 1 - remaining / cadenceDays, minHeight: 6),
        ),
      ]),
    );
  }
}
