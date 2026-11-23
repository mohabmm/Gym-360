import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../data/my_plan_view.dart';
import '../data/plan_providers.dart';
import 'plan_routes.dart';
import 'plan_today_card.dart';
import 'trainee_plan_strings.dart';

/// pt.training_plan: the trainee's plan week by week (done days ticked, the next one marked)
/// and their progress. Any day can be opened, e.g. to swap today's day for another.
class MyPlanScreen extends ConsumerWidget {
  const MyPlanScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final s = TraineePlanStrings(isArabic);
    return DefaultTabController(
      length: 2,
      child: RtlScaffold(
        isArabic: isArabic,
        appBar: AppBar(
          title: Text(s.myPlan),
          bottom: TabBar(tabs: [Tab(text: s.planTab), Tab(text: s.progressTab)]),
        ),
        body: TabBarView(children: [_PlanTab(s: s), _ProgressTab(s: s)]),
      ),
    );
  }
}

class _PlanTab extends ConsumerWidget {
  final TraineePlanStrings s;

  const _PlanTab({required this.s});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(myPlanProvider).when(
          data: (view) => view == null
              ? EmptyState(title: s.noPlan, subtitle: s.noPlanHint, icon: Icons.assignment_outlined)
              : RefreshIndicator(
                  onRefresh: () => ref.refresh(myPlanProvider.future),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      const PlanTodayCard(),
                      const SizedBox(height: 16),
                      Text(
                        '${s.goal(view.plan.summary.goal, view.plan.summary.goalText)} · '
                        '${s.weeks(view.plan.summary.durationWeeks)} · ${s.daysPerWeek(view.plan.summary.daysPerWeek)}',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                      const SizedBox(height: 8),
                      for (final week in _weeks(view)) _WeekTile(view: view, week: week, s: s),
                    ],
                  ),
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(message: s.loadError, onRetry: () => ref.invalidate(myPlanProvider), retryLabel: s.retry),
        );
  }

  static List<int> _weeks(MyPlanView view) => view.slots.map((slot) => slot.week).toSet().toList()..sort();
}

class _WeekTile extends StatelessWidget {
  final MyPlanView view;
  final int week;
  final TraineePlanStrings s;

  const _WeekTile({required this.view, required this.week, required this.s});

  @override
  Widget build(BuildContext context) {
    final slots = view.slots.where((slot) => slot.week == week).toList();
    final current = view.next?.week ?? (view.cursor.finished ? -1 : 1);
    final done = view.cursor.doneKeys.toSet();
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ExpansionTile(
        initiallyExpanded: week == current,
        title: Text(s.weekLabel(week)),
        subtitle: Text('${slots.where((slot) => done.contains(slot.day.key)).length}/${slots.length}'),
        children: [
          for (final slot in slots)
            ListTile(
              key: Key('plan-day-${slot.day.key}'),
              leading: Icon(_icon(slot, done), color: _color(context, slot, done)),
              title: Text(slot.day.displayTitle(s.isArabic)),
              subtitle: Text(s.exerciseCount(slot.day)),
              trailing: slot.day.key == view.cursor.nextDayKey ? Chip(label: Text(s.next)) : null,
              onTap: () => context.push(PlanRoutes.workout(slot.day.key)),
            ),
        ],
      ),
    );
  }

  IconData _icon(PlanSlot slot, Set<String> done) {
    if (done.contains(slot.day.key)) return Icons.check_circle_rounded;
    return slot.day.key == view.cursor.nextDayKey ? Icons.play_circle_fill_rounded : Icons.radio_button_unchecked;
  }

  Color _color(BuildContext context, PlanSlot slot, Set<String> done) =>
      done.contains(slot.day.key) ? Colors.green.shade600 : Theme.of(context).colorScheme.primary;
}

class _ProgressTab extends ConsumerWidget {
  final TraineePlanStrings s;

  const _ProgressTab({required this.s});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(myPlanProgressProvider).when(
          data: (progress) => RefreshIndicator(
            onRefresh: () => ref.refresh(myPlanProgressProvider.future),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _StatsRow(progress: progress, s: s),
                const SizedBox(height: 16),
                SectionHeader(title: s.weights, icon: Icons.trending_up_rounded),
                if (progress.exercises.isEmpty) Text(s.noWeights, style: Theme.of(context).textTheme.bodySmall),
                for (final trend in progress.exercises) _TrendTile(trend: trend, s: s),
                const SizedBox(height: 16),
                SectionHeader(title: s.history, icon: Icons.history_rounded),
                if (progress.history.isEmpty) Text(s.noHistory, style: Theme.of(context).textTheme.bodySmall),
                for (final workout in progress.history.where((w) => !w.isVoided)) _HistoryTile(workout: workout, s: s),
              ],
            ),
          ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(message: s.loadError, onRetry: () => ref.invalidate(myPlanProgressProvider), retryLabel: s.retry),
        );
  }
}

class _StatsRow extends StatelessWidget {
  final PlanProgressModel progress;
  final TraineePlanStrings s;

  const _StatsRow({required this.progress, required this.s});

  @override
  Widget build(BuildContext context) {
    final stats = progress.stats;
    final cursor = progress.cursor;
    return Row(
      children: [
        Expanded(child: StatCard(label: s.thisWeekLabel, value: '${stats.thisWeek}/${stats.weekTarget}', icon: Icons.calendar_view_week_rounded)),
        const SizedBox(width: 8),
        Expanded(child: StatCard(label: s.thisMonthLabel, value: '${stats.thisMonth}', icon: Icons.calendar_month_rounded)),
        if (cursor != null) ...[
          const SizedBox(width: 8),
          Expanded(child: StatCard(label: s.planDoneLabel, value: '${cursor.doneCount}/${cursor.total}', icon: Icons.flag_rounded)),
        ],
      ],
    );
  }
}

class _TrendTile extends StatelessWidget {
  final PlanExerciseTrendModel trend;
  final TraineePlanStrings s;

  const _TrendTile({required this.trend, required this.s});

  @override
  Widget build(BuildContext context) {
    final up = (trend.latest ?? 0) > (trend.first ?? 0);
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Icon(up ? Icons.arrow_upward_rounded : Icons.drag_handle_rounded, color: up ? Colors.green.shade600 : null),
      title: Text(trend.displayName(s.isArabic)),
      subtitle: Text(s.best(trend)),
      trailing: Text(s.weightChange(trend), style: Theme.of(context).textTheme.titleSmall),
    );
  }
}

class _HistoryTile extends StatelessWidget {
  final PlanWorkoutModel workout;
  final TraineePlanStrings s;

  const _HistoryTile({required this.workout, required this.s});

  @override
  Widget build(BuildContext context) {
    final date = DateFormat.MMMEd(s.isArabic ? 'ar' : 'en').format(workout.completedOn);
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: const Icon(Icons.check_circle_rounded, color: Colors.green),
      title: Text('${s.weekLabel(workout.week)} · ${workout.displayTitle(s.isArabic)}'),
      subtitle: Text('$date · ${s.setsDone(workout)}'),
      trailing: workout.fromCoachSession ? Chip(label: Text(s.withCoach)) : null,
    );
  }
}
