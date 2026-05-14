import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/nutrition_providers.dart';

class MealPlanScreen extends ConsumerWidget {
  const MealPlanScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final planAsync = ref.watch(activeMealPlanProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'خطة الوجبات' : 'Meal Plan'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(activeMealPlanProvider),
        child: planAsync.when(
          data: (plan) {
            if (plan == null) {
              return EmptyState(
                icon: Icons.restaurant_menu_outlined,
                title: isArabic
                    ? 'لا توجد خطة وجبات'
                    : 'No meal plan assigned',
                subtitle: isArabic
                    ? 'سيقوم مدربك بتعيين خطة وجبات لك'
                    : 'Your trainer will assign a meal plan',
              );
            }
            return _MealPlanContent(plan: plan, isArabic: isArabic);
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل خطة الوجبات. يرجى المحاولة مرة أخرى.'
                : 'Could not load the meal plan. Please try again.',
            onRetry: () => ref.invalidate(activeMealPlanProvider),
          ),
        ),
      ),
    );
  }
}

class _MealPlanContent extends StatelessWidget {
  final MealPlanModel plan;
  final bool isArabic;

  const _MealPlanContent({required this.plan, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final dayNames = isArabic
        ? [
            'الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء',
            'الخميس', 'الجمعة', 'السبت'
          ]
        : [
            'Sunday', 'Monday', 'Tuesday', 'Wednesday',
            'Thursday', 'Friday', 'Saturday'
          ];

    return CustomScrollView(
      slivers: [
        SliverToBoxAdapter(
          child: AppCard(
            margin: const EdgeInsets.all(16),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    plan.name ?? (isArabic ? 'خطة الوجبات' : 'Meal Plan'),
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  if (plan.description != null) ...[
                    const SizedBox(height: 4),
                    Text(plan.description!,
                        style: Theme.of(context).textTheme.bodySmall),
                  ],
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      _MacroBadge(
                        label: isArabic ? 'سعرات' : 'kcal',
                        value: '${plan.totalCalories ?? 0}',
                        color: Colors.orange,
                      ),
                      const SizedBox(width: 8),
                      _MacroBadge(
                        label: isArabic ? 'بروتين' : 'Protein',
                        value: '${(plan.totalProtein ?? 0).toStringAsFixed(0)}g',
                        color: Colors.blue,
                      ),
                      const SizedBox(width: 8),
                      _MacroBadge(
                        label: isArabic ? 'كارب' : 'Carbs',
                        value: '${(plan.totalCarbs ?? 0).toStringAsFixed(0)}g',
                        color: Colors.green,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),

        SliverList(
          delegate: SliverChildBuilderDelegate(
            (context, index) {
              final day = plan.days[index];
              // BE serialises dayOfWeek as the lowercase weekday name
              // ("monday".."sunday"); map it back to a display label by index.
              const orderedDayKeys = [
                'sunday', 'monday', 'tuesday', 'wednesday',
                'thursday', 'friday', 'saturday',
              ];
              final idx = orderedDayKeys.indexOf(day.dayOfWeek.toLowerCase());
              final dayName = idx >= 0 && idx < dayNames.length
                  ? dayNames[idx]
                  : (day.dayOfWeek.isEmpty
                      ? (isArabic ? 'يوم' : 'Day')
                      : day.dayOfWeek);
              return _DayCard(
                  day: day, dayName: dayName, isArabic: isArabic);
            },
            childCount: plan.days.length,
          ),
        ),

        const SliverToBoxAdapter(child: SizedBox(height: 24)),
      ],
    );
  }
}

class _MacroBadge extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _MacroBadge(
      {required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        '$value $label',
        style: TextStyle(
            fontSize: 12, color: color, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class _DayCard extends StatelessWidget {
  final MealPlanDayModel day;
  final String dayName;
  final bool isArabic;

  const _DayCard(
      {required this.day,
      required this.dayName,
      required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ExpansionTile(
        title: Text(dayName,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(
            '${day.totalCalories ?? 0} ${isArabic ? 'سعرة' : 'kcal'}'),
        children: day.meals.map((meal) {
          final mealName = isArabic && meal.nameAr != null
              ? meal.nameAr!
              : meal.name;
          return ListTile(
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 16, vertical: 0),
            title: Text(mealName),
            subtitle: Text(
                '${isArabic ? 'الوقت:' : 'Time:'} ${meal.time ?? '-'} • ${meal.totalCalories ?? 0} ${isArabic ? 'سعرة' : 'kcal'}'),
            leading: Icon(_mealIcon(meal.mealType),
                color: Theme.of(context).colorScheme.primary),
          );
        }).toList(),
      ),
    );
  }

  IconData _mealIcon(String type) {
    switch (type) {
      case 'breakfast':
        return Icons.free_breakfast;
      case 'lunch':
        return Icons.lunch_dining;
      case 'dinner':
        return Icons.dinner_dining;
      default:
        return Icons.fastfood;
    }
  }
}
