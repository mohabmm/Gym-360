import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/home_providers.dart';

class WeeklyActivityChart extends ConsumerWidget {
  const WeeklyActivityChart({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final progressAsync = ref.watch(progressDashboardProvider);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(AppConstants.borderRadius),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'النشاط الأسبوعي' : 'Weekly Activity',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 16),
          progressAsync.when(
            data: (progress) {
              // BE dashboard surfaces current streaks but not a per-day bar
              // series yet; derive a minimal weekly view from the streak so the
              // chart isn't empty when the trainee is actively training.
              // The most-recent `streak` days up to and including today are
              // painted as "active".
              final today = DateTime.now().weekday - 1; // 0=Mon..6=Sun
              final streak = progress.currentWorkoutStreak.clamp(0, 7);
              bool isActive(int i) {
                if (streak == 0) return false;
                final diffFromToday = today - i;
                return diffFromToday >= 0 && diffFromToday < streak;
              }

              if (streak == 0) {
                return SizedBox(
                  height: 150,
                  child: Center(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      child: Text(
                        isArabic
                            ? 'سجّل أول تمرين لهذا الأسبوع لتبدأ المتابعة'
                            : 'Log your first workout this week to start tracking',
                        textAlign: TextAlign.center,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurface.withOpacity(0.6),
                        ),
                      ),
                    ),
                  ),
                );
              }

              return SizedBox(
                height: 150,
                child: BarChart(
                  BarChartData(
                    alignment: BarChartAlignment.spaceAround,
                    maxY: 1,
                    barTouchData: BarTouchData(enabled: false),
                    titlesData: FlTitlesData(
                      show: true,
                      topTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false),
                      ),
                      rightTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false),
                      ),
                      leftTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false),
                      ),
                      bottomTitles: AxisTitles(
                        sideTitles: SideTitles(
                          showTitles: true,
                          getTitlesWidget: (value, meta) {
                            const daysEn = [
                              'M', 'T', 'W', 'T', 'F', 'S', 'S'
                            ];
                            const daysAr = [
                              'ن', 'ث', 'ر', 'خ', 'ج', 'س', 'ح'
                            ];
                            final idx = value.toInt();
                            if (idx < 0 || idx >= 7) {
                              return const SizedBox.shrink();
                            }
                            return Text(
                              isArabic ? daysAr[idx] : daysEn[idx],
                              style: theme.textTheme.bodySmall,
                            );
                          },
                        ),
                      ),
                    ),
                    borderData: FlBorderData(show: false),
                    gridData: const FlGridData(show: false),
                    barGroups: List.generate(7, (i) {
                      final active = isActive(i);
                      final isToday = i == today;
                      return BarChartGroupData(
                        x: i,
                        barRods: [
                          BarChartRodData(
                            toY: active ? 0.8 : 0.15,
                            width: 24,
                            borderRadius: BorderRadius.circular(6),
                            color: isToday && active
                                ? theme.colorScheme.primary
                                : active
                                    ? theme.colorScheme.primary
                                        .withOpacity(0.55)
                                    : theme.colorScheme.onSurface
                                        .withOpacity(0.08),
                          ),
                        ],
                      );
                    }),
                  ),
                ),
              );
            },
            loading: () => const SizedBox(
              height: 150,
              child: Center(child: CircularProgressIndicator()),
            ),
            error: (_, __) => SizedBox(
              height: 150,
              child: Center(
                child: Text(
                  isArabic
                      ? 'تعذر تحميل البيانات'
                      : 'Could not load data',
                  style: theme.textTheme.bodySmall,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
