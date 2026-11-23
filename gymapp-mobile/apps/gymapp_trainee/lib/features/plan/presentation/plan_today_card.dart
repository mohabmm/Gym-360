import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/my_plan_view.dart';
import '../data/plan_providers.dart';
import '../data/workout_session.dart';
import 'plan_routes.dart';
import 'trainee_plan_strings.dart';

/// pt.training_plan home card: the next workout day of the plan, with this week's count.
/// Renders nothing when the coach has not given a plan (or plans are off).
class PlanTodayCard extends ConsumerWidget {
  const PlanTodayCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = TraineePlanStrings(Localizations.localeOf(context).languageCode == 'ar');
    return ref.watch(myPlanProvider).maybeWhen(
          data: (view) => view == null ? const SizedBox.shrink() : _Card(view: view, s: s),
          loading: () => const ShimmerStatCard(),
          orElse: () => const SizedBox.shrink(),
        );
  }
}

class _Card extends StatelessWidget {
  final MyPlanView view;
  final TraineePlanStrings s;

  const _Card({required this.view, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final next = view.next;
    return AppCard(
      key: const Key('plan-today-card'),
      onTap: () => context.push(PlanRoutes.plan),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Header(view: view, s: s),
          const SizedBox(height: 10),
          if (view.cursor.finished)
            Text(s.planFinished, style: theme.textTheme.bodyMedium)
          else if (next != null)
            _NextDay(slot: next, s: s, doneToday: view.cursor.doneToday),
          const SizedBox(height: 12),
          _Progress(view: view, s: s),
          if (view.pendingCount > 0 || view.fromCache) ...[
            const SizedBox(height: 8),
            _OfflineNote(text: view.pendingCount > 0 ? s.pending(view.pendingCount) : s.offlineCopy),
          ],
          if (next != null && !view.cursor.doneToday) ...[
            const SizedBox(height: 12),
            AppButton(
              key: const Key('plan-start-workout'),
              text: s.start,
              icon: Icons.play_arrow_rounded,
              isGradient: true,
              onPressed: () => context.push(PlanRoutes.workout(next.day.key)),
            ),
          ],
        ],
      ),
    );
  }
}

class _Header extends StatelessWidget {
  final MyPlanView view;
  final TraineePlanStrings s;

  const _Header({required this.view, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(Icons.fitness_center_rounded, color: theme.colorScheme.primary),
        const SizedBox(width: 8),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(s.todaysWorkout, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              Text(view.plan.summary.displayName(s.isArabic),
                  style: theme.textTheme.bodySmall, maxLines: 1, overflow: TextOverflow.ellipsis),
            ],
          ),
        ),
        Icon(Directionality.of(context) == TextDirection.rtl ? Icons.chevron_left : Icons.chevron_right),
      ],
    );
  }
}

class _NextDay extends StatelessWidget {
  final PlanSlot slot;
  final TraineePlanStrings s;
  final bool doneToday;

  const _NextDay({required this.slot, required this.s, required this.doneToday});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final title = '${s.weekLabel(slot.week)} · ${slot.day.displayTitle(s.isArabic)}';
    final detail = s.exercisesAndMinutes(slot.day.exercises.length, WorkoutSession.estimatedMinutes(slot.day));
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (doneToday) Text(s.doneToday, style: theme.textTheme.titleSmall?.copyWith(color: Colors.green.shade700)),
        Text(doneToday ? s.nextTime(title) : title, style: theme.textTheme.titleSmall),
        Text(detail, style: theme.textTheme.bodySmall),
      ],
    );
  }
}

class _Progress extends StatelessWidget {
  final MyPlanView view;
  final TraineePlanStrings s;

  const _Progress({required this.view, required this.s});

  @override
  Widget build(BuildContext context) {
    final cursor = view.cursor;
    final total = cursor.total == 0 ? 1 : cursor.total;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        LinearProgressIndicator(value: cursor.doneCount / total, minHeight: 6, borderRadius: BorderRadius.circular(3)),
        const SizedBox(height: 6),
        Row(
          children: [
            Expanded(child: Text(s.doneOf(cursor.doneCount, cursor.total), style: Theme.of(context).textTheme.bodySmall)),
            Text(s.thisWeek(view.stats.thisWeek, view.stats.weekTarget), style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ],
    );
  }
}

class _OfflineNote extends StatelessWidget {
  final String text;

  const _OfflineNote({required this.text});

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Icon(Icons.cloud_off_rounded, size: 16, color: Theme.of(context).colorScheme.outline),
          const SizedBox(width: 6),
          Expanded(child: Text(text, style: Theme.of(context).textTheme.bodySmall)),
        ],
      );
}
