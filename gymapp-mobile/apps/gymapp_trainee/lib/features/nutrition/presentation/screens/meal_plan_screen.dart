import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../diet/diet_providers.dart';

/// The diet the coach gave: the daily target, then each day of the week (today first) with
/// every meal's foods and portions and its alternative. The diet notification opens here.
class MealPlanScreen extends ConsumerWidget {
  const MealPlanScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = DietStrings(Localizations.localeOf(context).languageCode == 'ar');
    final planAsync = ref.watch(myDietPlanProvider);
    return RtlScaffold(
      isArabic: s.isArabic,
      appBar: AppBar(title: Text(s.isArabic ? 'خطة الأكل' : 'Diet plan')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(myDietPlanProvider.future),
        child: planAsync.when(
          data: (plan) => plan == null || plan.days.isEmpty
              ? ListView(children: [
                  EmptyState(
                    icon: Icons.restaurant_menu_outlined,
                    title: s.isArabic ? 'لا توجد خطة أكل بعد' : 'No diet plan yet',
                    subtitle: s.isArabic ? 'الكابتن هيجهّز لك خطة على قد احتياجك' : 'Your coach will make one for your needs',
                  ),
                ])
              : _PlanView(plan: plan, s: s),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(
            message: s.isArabic ? 'تعذر تحميل خطة الأكل. حاول مرة أخرى.' : 'Could not load the diet plan. Please try again.',
            onRetry: () => ref.invalidate(myDietPlanProvider),
          ),
        ),
      ),
    );
  }
}

class _PlanView extends StatefulWidget {
  final DietPlanModel plan;
  final DietStrings s;

  const _PlanView({required this.plan, required this.s});

  @override
  State<_PlanView> createState() => _PlanViewState();
}

class _PlanViewState extends State<_PlanView> {
  late int _day = _todayIndex();

  int _todayIndex() {
    final today = DateTime.now().weekday % 7;
    final index = widget.plan.days.indexWhere((d) => d.weekdayIndex == today);
    return index < 0 ? 0 : index;
  }

  @override
  Widget build(BuildContext context) {
    final s = widget.s;
    final plan = widget.plan;
    final day = plan.days[_day];
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      children: [
        _TargetCard(plan: plan, s: s),
        const SizedBox(height: 12),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(children: [
            for (var i = 0; i < plan.days.length; i++)
              Padding(
                padding: const EdgeInsetsDirectional.only(end: 6),
                child: ChoiceChip(
                  label: Text(s.weekday(plan.days[i].weekdayIndex)),
                  selected: i == _day,
                  onSelected: (_) => setState(() => _day = i),
                ),
              ),
          ]),
        ),
        const SizedBox(height: 8),
        for (var i = 0; i < day.meals.length; i++) _MealCard(meal: day.meals[i], alternative: day.alternativeAt(i), s: s),
      ],
    );
  }
}

class _TargetCard extends StatelessWidget {
  final DietPlanModel plan;
  final DietStrings s;

  const _TargetCard({required this.plan, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(plan.displayName(s.isArabic), style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 4),
        Text('${s.dailyTarget}: ${s.kcal(plan.dailyCalories)}', style: theme.textTheme.bodyMedium),
        const SizedBox(height: 8),
        Wrap(spacing: 8, runSpacing: 6, children: [
          _Badge('${s.protein} ${s.grams(plan.proteinGrams)}', Colors.blue),
          _Badge('${s.carbs} ${s.grams(plan.carbsGrams)}', Colors.green),
          _Badge('${s.fat} ${s.grams(plan.fatGrams)}', Colors.orange),
        ]),
      ]),
    );
  }
}

class _Badge extends StatelessWidget {
  final String text;
  final Color color;

  const _Badge(this.text, this.color);

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
        child: Text(text, style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w600)),
      );
}

class _MealCard extends StatelessWidget {
  final DietMealModel meal;
  final DietMealModel? alternative;
  final DietStrings s;

  const _MealCard({required this.meal, required this.alternative, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      margin: const EdgeInsets.only(bottom: 10),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Expanded(child: Text(meal.displayName(s.isArabic), style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700))),
          Text(s.kcal(meal.calories), style: theme.textTheme.bodySmall),
        ]),
        Text(s.mealType(meal.mealType), style: theme.textTheme.bodySmall),
        const SizedBox(height: 6),
        for (final food in meal.foods) _FoodLine(food: food, s: s),
        if (alternative != null) ...[
          const Divider(height: 16),
          Text('${s.alternative}: ${alternative!.displayName(s.isArabic)} · ${s.kcal(alternative!.calories)}',
              style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600)),
          for (final food in alternative!.foods) _FoodLine(food: food, s: s),
        ],
      ]),
    );
  }
}

class _FoodLine extends StatelessWidget {
  final DietFoodModel food;
  final DietStrings s;

  const _FoodLine({required this.food, required this.s});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(children: [
          const Text('•  '),
          Expanded(child: Text(food.displayName(s.isArabic))),
          Text(s.amount(food), style: Theme.of(context).textTheme.bodySmall),
        ]),
      );
}
