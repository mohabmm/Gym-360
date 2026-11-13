import 'package:flutter/material.dart';

import '../../models/body_metric_model.dart';
import '../../widgets/empty_state.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import 'check_in_timeline.dart';
import 'day_one_comparison_card.dart';
import 'journey_hero_card.dart';
import 'metric_trend_card.dart';

/// Who is looking: the member at their own progress, or a coach at a client's.
sealed class JourneyViewer {
  const JourneyViewer();

  String measuredBy(BodyMetricEntryModel entry, ProgressJourneyStrings s);

  /// Label for entries the member logged themself.
  String selfLabel(ProgressJourneyStrings s);
}

/// The member. [coachId]/[coachName] name the assigned coach on coach entries.
class MemberViewer extends JourneyViewer {
  final String? coachId;
  final String? coachName;

  const MemberViewer({this.coachId, this.coachName});

  @override
  String selfLabel(ProgressJourneyStrings s) => s.you;

  @override
  String measuredBy(BodyMetricEntryModel entry, ProgressJourneyStrings s) => switch (ProgressJourney.measuredBy(entry)) {
        MeasuredBy.member => s.you,
        MeasuredBy.inBody => s.inBody,
        MeasuredBy.coach => _named(entry, s),
      };

  String _named(BodyMetricEntryModel entry, ProgressJourneyStrings s) {
    final name = coachName?.trim() ?? '';
    return name.isNotEmpty && entry.loggedBy == coachId ? s.coachNamed(name) : s.coach;
  }
}

/// A coach viewing a client; entries they logged show as "You".
class CoachViewer extends JourneyViewer {
  final String coachId;

  const CoachViewer({required this.coachId});

  @override
  String selfLabel(ProgressJourneyStrings s) => s.client;

  @override
  String measuredBy(BodyMetricEntryModel entry, ProgressJourneyStrings s) => switch (ProgressJourney.measuredBy(entry)) {
        MeasuredBy.member => s.client,
        MeasuredBy.inBody => s.inBody,
        MeasuredBy.coach => entry.loggedBy == coachId ? s.you : s.coach,
      };
}

/// pt.progress_measurements: progress from day one — hero, trend chart, day 1 vs now, check-ins.
/// Not scrollable itself; put it in a ListView / SliverToBoxAdapter.
class ProgressJourneyView extends StatelessWidget {
  final ProgressJourney journey;
  final bool isArabic;
  final JourneyViewer viewer;
  final VoidCallback? onEditGoal;
  final VoidCallback? onRecordMeasurements;
  final DateTime? now;

  const ProgressJourneyView({
    super.key,
    required this.journey,
    required this.isArabic,
    required this.viewer,
    this.onEditGoal,
    this.onRecordMeasurements,
    this.now,
  });

  @override
  Widget build(BuildContext context) {
    final s = ProgressJourneyStrings(isArabic);
    if (journey.isEmpty) return _empty(s);
    final today = now ?? DateTime.now();
    const gap = SizedBox(height: 12);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        JourneyHeroCard(journey: journey, strings: s, now: today, onEditGoal: onEditGoal),
        gap,
        MetricTrendCard(journey: journey, strings: s, now: today, selfLabel: viewer.selfLabel(s)),
        if (!journey.hasOnlyBaseline) ...[
          gap,
          DayOneComparisonCard(journey: journey, strings: s),
        ],
        gap,
        CheckInTimeline(journey: journey, strings: s, measuredByLabel: (e) => viewer.measuredBy(e, s)),
      ],
    );
  }

  Widget _empty(ProgressJourneyStrings s) {
    final coach = viewer is CoachViewer;
    return EmptyState(
      icon: Icons.straighten,
      title: coach ? s.coachEmptyTitle : s.memberEmptyTitle,
      subtitle: coach ? s.coachEmptyBody : s.memberEmptyBody,
      actionLabel: coach && onRecordMeasurements != null ? s.recordMeasurements : null,
      onAction: coach ? onRecordMeasurements : null,
    );
  }
}
