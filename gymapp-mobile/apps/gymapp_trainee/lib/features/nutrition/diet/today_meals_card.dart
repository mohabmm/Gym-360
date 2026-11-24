import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import 'diet_providers.dart';

/// Home card «أكل النهارده»: today's meals of the coach's diet with a one-tap tick, so the
/// member opens the app every day; the coach sees the ticks before the next session.
/// Renders nothing until the coach gives a diet.
class TodayMealsCard extends ConsumerWidget {
  const TodayMealsCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return ref.watch(todayMealsProvider).maybeWhen(
          data: (today) => today == null || today.meals.isEmpty
              ? const SizedBox.shrink()
              : _Card(today: today, s: _Strings(isArabic)),
          orElse: () => const SizedBox.shrink(),
        );
  }
}

class _Strings extends DietStrings {
  const _Strings(super.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get title => _t('أكل النهارده', "Today's meals");
  String get ate => _t('أكلتها ✓', 'Ate it ✓');
  String get ateSwap => _t('أكلت البديل', 'Ate the swap');
  String get allDone => _t('برافو! يوم كامل على الخطة 💪', 'Well done! A full day on plan 💪');
  String get coachFollows => _t('الكابتن بيتابع معاك في الحصة الجاية', 'Your coach follows up at your next session');
  String progress(TodayMealsModel t) =>
      _t('${t.doneCount} من ${t.meals.length} وجبات · ${t.eatenCalories} من ${t.targetCalories} سعرة',
          '${t.doneCount} of ${t.meals.length} meals · ${t.eatenCalories} of ${t.targetCalories} kcal');
}

class _Card extends ConsumerWidget {
  final TodayMealsModel today;
  final _Strings s;

  const _Card({required this.today, required this.s});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final target = today.targetCalories == 0 ? 1 : today.targetCalories;
    return AppCard(
      key: const Key('today-meals-card'),
      onTap: () => context.push(AppRoutes.mealPlan),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Icon(Icons.restaurant_rounded, color: theme.colorScheme.primary),
          const SizedBox(width: 8),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(s.title, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              Text(today.planName, style: theme.textTheme.bodySmall, maxLines: 1, overflow: TextOverflow.ellipsis),
            ]),
          ),
        ]),
        const SizedBox(height: 10),
        LinearProgressIndicator(
          value: (today.eatenCalories / target).clamp(0, 1).toDouble(),
          minHeight: 6,
          borderRadius: BorderRadius.circular(3),
        ),
        const SizedBox(height: 6),
        Text(today.allDone ? s.allDone : s.progress(today), style: theme.textTheme.bodySmall),
        const SizedBox(height: 4),
        for (final meal in today.meals) _MealRow(meal: meal, s: s),
        const SizedBox(height: 6),
        Text(s.coachFollows, style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
      ]),
    );
  }
}

class _MealRow extends ConsumerWidget {
  final TodayMealModel meal;
  final _Strings s;

  const _MealRow({required this.meal, required this.s});

  Future<void> _ate(BuildContext context, WidgetRef ref, {bool alternative = false}) async {
    try {
      await ref.read(dietActionsProvider).ate(meal.index, alternative: alternative);
    } on ApiException catch (e) {
      if (context.mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final m = meal.meal;
    return ListTile(
      key: Key('today-meal-${meal.index}'),
      contentPadding: EdgeInsets.zero,
      dense: true,
      leading: Icon(meal.done ? Icons.check_circle : Icons.radio_button_unchecked,
          color: meal.done ? Colors.green.shade600 : theme.colorScheme.outline),
      title: Text(m.displayName(s.isArabic), maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: Text('${s.mealType(m.mealType)} · ${s.kcal(m.calories)}'),
      trailing: meal.done
          ? null
          : PopupMenuButton<bool>(
              key: Key('today-meal-ate-${meal.index}'),
              tooltip: s.ate,
              icon: const Icon(Icons.check),
              onSelected: (alternative) => _ate(context, ref, alternative: alternative),
              itemBuilder: (_) => [
                PopupMenuItem(value: false, child: Text(s.ate)),
                if (meal.alternative != null)
                  PopupMenuItem(value: true, child: Text('${s.ateSwap}: ${meal.alternative!.displayName(s.isArabic)}')),
              ],
            ),
    );
  }
}
