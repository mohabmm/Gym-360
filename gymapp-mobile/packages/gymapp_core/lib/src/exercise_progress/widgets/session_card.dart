import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../exercise_progress_strings.dart';
import '../session_insights.dart';
import '../session_strings.dart';
import '../session_summary_model.dart';
import 'exercise_history_sheet.dart';
import 'progress_badge.dart';

/// One logged session: heading, wins, every exercise in the coach's order with its change since
/// last time, and the coach's recap. Folded to one line until tapped.
class SessionCard extends StatefulWidget {
  final SessionRecap recap;
  final ExerciseProgressStrings strings;
  final bool initiallyExpanded;

  /// Opens the full session (member app); no link when null.
  final ValueChanged<String>? onOpenSession;

  const SessionCard({
    super.key,
    required this.recap,
    required this.strings,
    this.initiallyExpanded = false,
    this.onOpenSession,
  });

  @override
  State<SessionCard> createState() => _SessionCardState();
}

class _SessionCardState extends State<SessionCard> {
  late bool _expanded = widget.initiallyExpanded;

  @override
  Widget build(BuildContext context) {
    final recap = widget.recap;
    final strings = widget.strings;
    return AppCard(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      onTap: () => setState(() => _expanded = !_expanded),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _Heading(session: recap.session, strings: strings, expanded: _expanded),
          const SizedBox(height: 6),
          if (_expanded) ..._details(recap, strings) else Text(strings.collapsedLine(recap)),
        ],
      ),
    );
  }

  List<Widget> _details(SessionRecap recap, ExerciseProgressStrings strings) => [
        if (recap.hasWins) _Wins(recap: recap, strings: strings),
        for (final row in recap.rows) _ExerciseRow(row: row, strings: strings),
        if (recap.session.coachRecap != null) _CoachRecap(recap: recap.session.coachRecap!, strings: strings),
        if (widget.onOpenSession != null)
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: TextButton(
              onPressed: () => widget.onOpenSession!(recap.session.sessionId),
              child: Text(strings.sessionDetails),
            ),
          ),
      ];
}

class _Heading extends StatelessWidget {
  final SessionSummaryModel session;
  final ExerciseProgressStrings strings;
  final bool expanded;

  const _Heading({required this.session, required this.strings, required this.expanded});

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Expanded(
            child: Text(
              strings.sessionHeading(session),
              style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700),
            ),
          ),
          Icon(expanded ? Icons.expand_less : Icons.expand_more, color: AppColors.grey400),
        ],
      );
}

class _Wins extends StatelessWidget {
  final SessionRecap recap;
  final ExerciseProgressStrings strings;

  const _Wins({required this.recap, required this.strings});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: Wrap(
          spacing: 6,
          runSpacing: 6,
          children: [
            if (recap.personalBests > 0)
              ProgressBadge(strings.bests(recap.personalBests), AppColors.accentGold, icon: Icons.emoji_events),
            if (recap.improved > 0)
              ProgressBadge(strings.improvedCount(recap.improved), AppColors.success, icon: Icons.trending_up),
            if (recap.targetsHit > 0)
              ProgressBadge(strings.targetsHitCount(recap.targetsHit), AppColors.info, icon: Icons.flag),
          ],
        ),
      );
}

/// "Bench press  👑  +2.5 kg" over "12×60 · 10×70 kg", with the coach's notes; tap for history.
class _ExerciseRow extends StatelessWidget {
  final SessionExerciseRow row;
  final ExerciseProgressStrings strings;

  const _ExerciseRow({required this.row, required this.strings});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    final entry = row.entry;
    return InkWell(
      onTap: () => showExerciseHistorySheet(context, exercise: row.exercise, strings: strings),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(row.exercise.displayName(strings.isArabic), style: text.titleSmall)),
                if (entry.isPersonalBest) ...[
                  const Icon(Icons.emoji_events, size: 16, color: AppColors.accentGold),
                  const SizedBox(width: 6),
                ],
                DeltaBadge(delta: row.delta, strings: strings),
              ],
            ),
            const SizedBox(height: 2),
            Text(strings.entryLine(entry), style: text.bodyMedium),
            for (final note in strings.setNotes(entry)) _Note(note),
            if (entry.notes?.trim().isNotEmpty ?? false) _Note('${strings.coachNote}: ${entry.notes!.trim()}'),
          ],
        ),
      ),
    );
  }
}

/// The change since last time, green when it went up.
class DeltaBadge extends StatelessWidget {
  final ExerciseDelta delta;
  final ExerciseProgressStrings strings;
  final String? prefix;

  const DeltaBadge({super.key, required this.delta, required this.strings, this.prefix});

  @override
  Widget build(BuildContext context) {
    final color = delta.isUp ? AppColors.success : (delta.isFirstTime ? AppColors.info : AppColors.grey600);
    final label = strings.delta(delta);
    return ProgressBadge(prefix == null ? label : '$prefix: $label', color, icon: delta.isUp ? Icons.arrow_upward : null);
  }
}

class _Note extends StatelessWidget {
  final String text;

  const _Note(this.text);

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 2),
        child: Text(text, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.grey600)),
      );
}

/// The coach's words after the session, with how hard the member worked.
class _CoachRecap extends StatelessWidget {
  final SessionCoachRecapModel recap;
  final ExerciseProgressStrings strings;

  const _CoachRecap({required this.recap, required this.strings});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.format_quote, size: 18, color: AppColors.primary),
              const SizedBox(width: 6),
              Expanded(child: Text(strings.coachSays, style: text.labelMedium?.copyWith(color: AppColors.primary))),
              if (recap.effortRating > 0) _EffortDots(rating: recap.effortRating, label: strings.effort),
            ],
          ),
          if (recap.hasFeedback) ...[const SizedBox(height: 6), Text(recap.feedback!, style: text.bodyMedium)],
          if (recap.hasFocus) ...[
            const SizedBox(height: 6),
            Text('${strings.focus}: ${recap.nextSessionFocus!}', style: text.bodySmall),
          ],
        ],
      ),
    );
  }
}

class _EffortDots extends StatelessWidget {
  static const _max = 5;

  final int rating;
  final String label;

  const _EffortDots({required this.rating, required this.label});

  @override
  Widget build(BuildContext context) => Semantics(
        label: '$label $rating/$_max',
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('$label ', style: Theme.of(context).textTheme.labelSmall?.copyWith(color: AppColors.grey500)),
            for (var i = 0; i < _max; i++)
              Icon(Icons.circle, size: 8, color: i < rating ? AppColors.primary : AppColors.grey400),
          ],
        ),
      );
}
