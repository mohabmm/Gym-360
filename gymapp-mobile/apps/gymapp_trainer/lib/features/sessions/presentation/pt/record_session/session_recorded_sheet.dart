import 'package:flutter/material.dart';

import 'record_session_strings.dart';
import 'session_time_draft.dart';

/// What the coach can do right after recording (each null when its feature is off).
class NextSteps {
  final VoidCallback? exercises;
  final VoidCallback? measurements;

  const NextSteps({this.exercises, this.measurements});
}

/// After recording: what was saved, sessions left, who confirms, and clear next steps.
class SessionRecordedSheet extends StatelessWidget {
  final RecordedSession result;
  final String clientName;
  final RecordSessionStrings strings;
  final SessionTimeDraft draft;
  final NextSteps next;

  const SessionRecordedSheet({
    super.key,
    required this.result,
    required this.clientName,
    required this.strings,
    required this.draft,
    required this.next,
  });

  void _go(BuildContext context, VoidCallback action) {
    Navigator.of(context).pop();
    action();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final at = result.startAt;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(result.alreadyLogged ? strings.alreadyTitle : strings.recordedTitle,
                textAlign: TextAlign.center,
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 8),
            if (at != null)
              Text(strings.recordedAt(context, at, draft), textAlign: TextAlign.center, style: theme.textTheme.bodyLarge),
            const SizedBox(height: 4),
            Text(
              result.alreadyLogged ? strings.alreadyBody : '${strings.left(result.remaining)} · ${strings.willConfirm(clientName)}',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium,
            ),
            const SizedBox(height: 20),
            if (next.exercises case final go?)
              FilledButton.icon(
                key: const ValueKey('next-exercises'),
                onPressed: () => _go(context, go),
                icon: const Icon(Icons.fitness_center),
                label: Text(strings.recordExercises),
              ),
            if (next.measurements case final go?) ...[
              const SizedBox(height: 8),
              OutlinedButton.icon(
                key: const ValueKey('next-measurements'),
                onPressed: () => _go(context, go),
                icon: const Icon(Icons.straighten),
                label: Text(strings.recordMeasurements),
              ),
            ],
            const SizedBox(height: 8),
            TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(strings.done)),
          ],
        ),
      ),
    );
  }
}
