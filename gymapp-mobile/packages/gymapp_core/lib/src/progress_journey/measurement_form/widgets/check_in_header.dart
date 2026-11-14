import 'package:flutter/material.dart';

import '../../../theme/app_colors.dart';
import '../../../widgets/app_card.dart';
import '../measurement_form_strings.dart';

/// Last check-in (or "first check-in"), the date chip, and the "already recorded" warning.
class CheckInHeader extends StatelessWidget {
  final MeasurementFormStrings strings;
  final DateTime? lastCheckIn;
  final DateTime date;
  final DateTime now;
  final bool alreadyRecorded;
  final ValueChanged<DateTime> onDate;

  /// How far back a check-in can be dated.
  static const int maxDaysBack = 30;

  const CheckInHeader({
    super.key,
    required this.strings,
    required this.lastCheckIn,
    required this.date,
    required this.now,
    required this.alreadyRecorded,
    required this.onDate,
  });

  Future<void> _pick(BuildContext context) async {
    final today = DateTime(now.year, now.month, now.day);
    final picked = await showDatePicker(
      context: context,
      initialDate: date,
      firstDate: today.subtract(const Duration(days: maxDaysBack)),
      lastDate: today,
      helpText: strings.checkInDate,
    );
    if (picked != null) onDate(picked);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final last = lastCheckIn;
    return AppCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(children: [
            Icon(last == null ? Icons.flag_outlined : Icons.history, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Expanded(
              child: Text(last == null ? strings.firstCheckIn : strings.lastCheckIn(last, now),
                  style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
            ),
            ActionChip(
              key: const ValueKey('check-in-date'),
              avatar: const Icon(Icons.event, size: 18),
              label: Text(strings.dateLabel(date, now)),
              onPressed: () => _pick(context),
            ),
          ]),
          if (alreadyRecorded) ...[const SizedBox(height: 12), _Warning(text: strings.alreadyRecorded)],
        ],
      ),
    );
  }
}

class _Warning extends StatelessWidget {
  final String text;

  const _Warning({required this.text});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppColors.warning.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Row(children: [
          const Icon(Icons.info_outline, color: AppColors.warning, size: 20),
          const SizedBox(width: 8),
          Expanded(child: Text(text, style: Theme.of(context).textTheme.bodySmall)),
        ]),
      );
}
