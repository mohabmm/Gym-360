import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/nutrition_providers.dart';

class ClientNutritionViewScreen extends ConsumerWidget {
  final String clientId;

  const ClientNutritionViewScreen({super.key, required this.clientId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final nutritionAsync = ref.watch(clientNutritionProvider(clientId));

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تغذية العميل' : 'Client Nutrition'),
      ),
      body: RefreshIndicator(
        onRefresh: () async =>
            ref.invalidate(clientNutritionProvider(clientId)),
        child: nutritionAsync.when(
          data: (data) {
            final todayCalories =
                (data['todayCalories'] as num?)?.toInt() ?? 0;
            final targetCalories =
                (data['targetCalories'] as num?)?.toInt() ?? 2000;
            final todayWater = (data['todayWaterMl'] as num?)?.toInt() ?? 0;
            final logs = data['foodLogs'] as List? ?? [];

            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Today's overview
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isArabic ? 'سعرات اليوم' : "Today's Calories",
                          style: theme.textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 12),
                        Row(
                          mainAxisAlignment:
                              MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              '$todayCalories',
                              style: theme.textTheme.displaySmall
                                  ?.copyWith(
                                fontWeight: FontWeight.w700,
                                color: theme.colorScheme.primary,
                              ),
                            ),
                            Text(
                              '/ $targetCalories kcal',
                              style: theme.textTheme.bodyMedium
                                  ?.copyWith(color: AppColors.grey500),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        LinearProgressIndicator(
                          value: targetCalories > 0
                              ? (todayCalories / targetCalories).clamp(
                                  0.0, 1.0)
                              : 0.0,
                          backgroundColor: AppColors.grey200,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),

                // Water intake
                Card(
                  child: ListTile(
                    leading: const Icon(Icons.water_drop,
                        color: AppColors.info),
                    title: Text(isArabic ? 'ماء اليوم' : "Today's Water"),
                    trailing: Text(
                      '${(todayWater / 1000).toStringAsFixed(1)} L',
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: AppColors.info,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 16),

                Text(
                  isArabic ? 'سجل الطعام' : 'Food Log',
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 8),

                if (logs.isEmpty)
                  EmptyState(
                    icon: Icons.restaurant_outlined,
                    title: isArabic
                        ? 'لم يسجل الطعام اليوم'
                        : 'No food logged today',
                  )
                else
                  ...logs.map((log) {
                    final logMap = log as Map<String, dynamic>;
                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        leading: const Icon(Icons.fastfood_outlined),
                        title: Text(logMap['foodItemName'] as String? ??
                            (isArabic ? 'طعام' : 'Food')),
                        subtitle: Text(
                            '${logMap['quantity'] ?? ''} ${logMap['unit'] ?? 'g'}'),
                        trailing: Text(
                          '${logMap['calories'] ?? 0} kcal',
                          style: theme.textTheme.bodySmall
                              ?.copyWith(color: AppColors.grey500),
                        ),
                      ),
                    );
                  }),
              ],
            );
          },
          loading: () => ListView(
            padding: const EdgeInsets.all(16),
            children: const [
              ShimmerCard(height: 120),
              SizedBox(height: 12),
              ShimmerCard(height: 60),
              SizedBox(height: 12),
              ShimmerCard(height: 200),
            ],
          ),
          error: (e, _) => AppErrorWidget(
            message: e.toString(),
            onRetry: () =>
                ref.invalidate(clientNutritionProvider(clientId)),
          ),
        ),
      ),
    );
  }
}
