import 'package:gymapp_core/gymapp_core.dart';

import 'plan_workout_queue.dart';

/// The trainee's plan as the screens show it: the server's answer (or the cached copy when
/// offline) moved on by workouts finished on this phone that are not uploaded yet.
class MyPlanView {
  final MyPlanModel data;
  final PlanCursorModel cursor;
  final PlanWeekStatsModel stats;
  final int pendingCount;
  final bool fromCache;

  const MyPlanView({
    required this.data,
    required this.cursor,
    required this.stats,
    this.pendingCount = 0,
    this.fromCache = false,
  });

  TrainingPlanModel get plan => data.plan;

  List<PlanSlot> get slots => PlanSlot.of(plan.document);

  PlanSlot? get next => slotFor(cursor.nextDayKey);

  PlanSlot? slotFor(String? dayKey) => dayKey == null ? null : slots.where((s) => s.day.key == dayKey).firstOrNull;

  factory MyPlanView.build(MyPlanModel data, List<PendingPlanWorkout> pending, {DateTime? now, bool fromCache = false}) {
    final today = now ?? DateTime.now();
    final mine = pending.where((p) => p.planId == data.plan.summary.id).toList();
    final weekStart = PlanWeekStatsModel.weekStart(today);
    bool sameDay(DateTime a) => a.year == today.year && a.month == today.month && a.day == today.day;
    final local = [for (final p in mine) p.finishedAt.toLocal()];
    return MyPlanView(
      data: data,
      cursor: data.cursor.withPending(data.plan.document, mine.map((p) => p.dayKey), pendingToday: local.any(sameDay)),
      stats: data.stats.plus(
        week: local.where((d) => !d.isBefore(weekStart)).length,
        month: local.where((d) => d.year == today.year && d.month == today.month).length,
      ),
      pendingCount: mine.length,
      fromCache: fromCache,
    );
  }
}
