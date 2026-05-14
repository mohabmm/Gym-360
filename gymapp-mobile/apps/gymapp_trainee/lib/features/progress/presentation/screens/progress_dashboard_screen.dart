import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/progress_providers.dart';

class ProgressDashboardScreen extends ConsumerWidget {
  const ProgressDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final dashboardAsync = ref.watch(progressDashboardDetailProvider);
    final metricsAsync = ref.watch(bodyMetricsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقدمي' : 'My Progress'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: isArabic ? 'تسجيل قياس' : 'Log Measurement',
            onPressed: () => context.push(AppRoutes.logWeight),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(progressDashboardDetailProvider);
          ref.invalidate(bodyMetricsProvider);
          ref.invalidate(progressPhotosProvider);
        },
        child: CustomScrollView(
          slivers: [
            // Streak card (from dashboard — no separate /progress/streaks endpoint)
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                child: dashboardAsync.when(
                  data: (dashboard) => _StreakCard(
                    currentStreak: dashboard.currentWorkoutStreak,
                    longestStreak: dashboard.longestWorkoutStreak,
                    isArabic: isArabic,
                  ),
                  loading: () => const ShimmerCard(height: 80),
                  error: (_, __) => const SizedBox.shrink(),
                ),
              ),
            ),

            // Stats row
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: dashboardAsync.when(
                  data: (dashboard) {
                    final weight = dashboard.latestMetrics?.weight;
                    final change = dashboard.weightChangeFromFirst;
                    return Row(
                      children: [
                        Expanded(
                          child: StatCard(
                            label: isArabic ? 'الوزن الحالي' : 'Current Weight',
                            value: weight != null
                                ? '${weight.toStringAsFixed(1)} kg'
                                : '-',
                            icon: Icons.monitor_weight_outlined,
                            iconColor: Colors.blue,
                            compact: true,
                            trend: change != null
                                ? '${change > 0 ? '+' : ''}${change.toStringAsFixed(1)}'
                                : null,
                            trendUp: (change ?? 0) > 0,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: StatCard(
                            label:
                                isArabic ? 'أيام الحضور' : 'Check-In Streak',
                            value: '${dashboard.currentCheckInStreak}',
                            icon: Icons.event_available,
                            iconColor: Colors.green,
                            compact: true,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: StatCard(
                            label: isArabic ? 'الصور' : 'Photos',
                            value: '${dashboard.photoCount}',
                            icon: Icons.photo_library_outlined,
                            iconColor: Colors.purple,
                            compact: true,
                          ),
                        ),
                      ],
                    );
                  },
                  loading: () => const ShimmerCard(height: 80),
                  error: (_, __) => const SizedBox.shrink(),
                ),
              ),
            ),

            // Weight chart — derives its series from the body-metrics list
            // (the /progress/dashboard DTO doesn't ship a trend array yet).
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: metricsAsync.when(
                  data: (metrics) => _WeightChart(
                    trend: _weightTrendFromMetrics(metrics),
                    isArabic: isArabic,
                  ),
                  loading: () => const ShimmerCard(height: 200),
                  error: (_, __) => const SizedBox.shrink(),
                ),
              ),
            ),

            // Quick actions
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: Row(
                  children: [
                    Expanded(
                      child: _ActionCard(
                        icon: Icons.straighten,
                        label: isArabic ? 'قياسات الجسم' : 'Measurements',
                        onTap: () => context.push(AppRoutes.logMeasurements),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _ActionCard(
                        icon: Icons.photo_camera,
                        label: isArabic ? 'صور التقدم' : 'Progress Photos',
                        onTap: () => context.push(AppRoutes.progressPhotos),
                      ),
                    ),
                  ],
                ),
              ),
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 24)),
          ],
        ),
      ),
    );
  }
}

/// Projects the body-metrics list to an ordered weight trend. Entries without
/// a weight are skipped, and invalid dates fall back to `createdAt`.
List<WeightTrendPoint> _weightTrendFromMetrics(
    List<BodyMetricEntryModel> metrics) {
  final points = <WeightTrendPoint>[];
  for (final m in metrics) {
    if (m.weight == null) continue;
    final parsed = DateTime.tryParse(m.date) ?? m.createdAt;
    points.add(WeightTrendPoint(date: parsed, weight: m.weight!));
  }
  points.sort((a, b) => a.date.compareTo(b.date));
  return points;
}

class _StreakCard extends StatelessWidget {
  final int currentStreak;
  final int longestStreak;
  final bool isArabic;

  const _StreakCard({
    required this.currentStreak,
    required this.longestStreak,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            const Icon(Icons.local_fire_department,
                color: Colors.orange, size: 36),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    isArabic ? 'السلسلة الحالية' : 'Current Streak',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  Text(
                    '$currentStreak ${isArabic ? 'أيام' : 'days'}',
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                          color: Colors.orange,
                        ),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  isArabic ? 'أطول سلسلة' : 'Longest',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                Text(
                  '$longestStreak ${isArabic ? 'أيام' : 'days'}',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _WeightChart extends StatelessWidget {
  final List<WeightTrendPoint> trend;
  final bool isArabic;

  const _WeightChart({required this.trend, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    if (trend.isEmpty) {
      return AppCard(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Center(
            child: Text(
              isArabic
                  ? 'لا توجد بيانات وزن بعد'
                  : 'No weight data yet',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
        ),
      );
    }

    final spots = trend.asMap().entries.map((e) {
      return FlSpot(e.key.toDouble(), e.value.weight);
    }).toList();

    final minY = trend.map((e) => e.weight).reduce((a, b) => a < b ? a : b) - 2;
    final maxY = trend.map((e) => e.weight).reduce((a, b) => a > b ? a : b) + 2;

    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isArabic ? 'منحنى الوزن' : 'Weight Trend',
              style: Theme.of(context)
                  .textTheme
                  .titleSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 16),
            SizedBox(
              height: 160,
              child: LineChart(
                LineChartData(
                  minY: minY,
                  maxY: maxY,
                  lineBarsData: [
                    LineChartBarData(
                      spots: spots,
                      isCurved: true,
                      color: Theme.of(context).colorScheme.primary,
                      barWidth: 2.5,
                      dotData: const FlDotData(show: false),
                      belowBarData: BarAreaData(
                        show: true,
                        color: Theme.of(context)
                            .colorScheme
                            .primary
                            .withOpacity(0.1),
                      ),
                    ),
                  ],
                  gridData: const FlGridData(show: false),
                  borderData: FlBorderData(show: false),
                  titlesData: const FlTitlesData(show: false),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ActionCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _ActionCard(
      {required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Icon(icon,
                  size: 28,
                  color: Theme.of(context).colorScheme.primary),
              const SizedBox(height: 8),
              Text(
                label,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
