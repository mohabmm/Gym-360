import 'package:flutter/material.dart';

import '../../../theme/app_colors.dart';
import '../measurement_form_strings.dart';

/// Sticky bottom bar: how many values are filled, why Save is off, and the Save button.
class MeasurementSaveBar extends StatelessWidget {
  final MeasurementFormStrings strings;
  final int filled;

  /// Shown instead of the count while Save is disabled (null = can save).
  final String? blockedReason;
  final bool saving;
  final VoidCallback onSave;

  const MeasurementSaveBar({
    super.key,
    required this.strings,
    required this.filled,
    required this.blockedReason,
    required this.saving,
    required this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final reason = blockedReason;
    return Material(
      elevation: 8,
      color: theme.colorScheme.surface,
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
          child: Row(children: [
            Expanded(
              child: Text(
                reason ?? strings.filled(filled),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: reason == null ? AppColors.grey500 : AppColors.error,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const SizedBox(width: 12),
            FilledButton.icon(
              key: const ValueKey('save-measurements'),
              onPressed: saving || reason != null ? null : onSave,
              icon: saving
                  ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.check),
              label: Text(strings.saveCount(filled)),
            ),
          ]),
        ),
      ),
    );
  }
}
