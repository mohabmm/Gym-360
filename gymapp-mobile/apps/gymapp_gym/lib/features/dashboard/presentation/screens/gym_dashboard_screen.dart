import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/dashboard_provider.dart';

class GymDashboardScreen extends ConsumerWidget {
  const GymDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(dashboardProvider);
    final gym = ref.watch(activeGymProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final pagePad = context.responsivePagePadding;

    final gymName = gym.valueOrNull?.name ?? (isArabic ? 'الصالة' : 'Gym');
    final hasGym = ref.watch(currentGymIdProvider).isNotEmpty;
    final canViewRevenue = ref
            .watch(authProvider)
            .valueOrNull
            ?.hasPermission(StaffPermission.viewRevenue) ??
        false;

    if (!hasGym) {
      // Zeros here would read as "an empty gym"; say what is actually wrong
      // and point at Settings, where sign-out lives.
      return RtlScaffold(
        isArabic: isArabic,
        appBar: AppBar(title: Text(gymName)),
        body: EmptyState(
          icon: Icons.store_mall_directory_outlined,
          title: isArabic
              ? 'هذا الحساب غير مرتبط بصالة'
              : 'This account is not linked to a gym',
          subtitle: isArabic
              ? 'اطلب من مالك الصالة اضافتك كموظف ثم سجل الدخول مرة اخرى.'
              : 'Ask the gym owner to add you as staff, then sign in again.',
          actionLabel: isArabic ? 'الاعدادات' : 'Settings',
          onAction: () => context.push(RoutePaths.gymProfile),
        ),
      );
    }

    return RtlScaffold(
      isArabic: isArabic,
      appBar: AppBar(
        title: Text(gymName),
        actions: [
          IconButton(
            icon: const Icon(Icons.tablet_android),
            tooltip: isArabic ? 'وضع الكيوسك' : 'Kiosk Mode',
            onPressed: () => context.go(RoutePaths.kiosk),
          ),
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            tooltip: isArabic ? 'الاعلانات' : 'Announcements',
            onPressed: () => context.push(RoutePaths.announcements),
          ),
        ],
      ),
      body: dashboard.when(
        data: (data) => RefreshIndicator(
          onRefresh: () => ref.refresh(dashboardProvider.future),
          child: ListView(
            padding: EdgeInsets.all(pagePad),
            children: [
              // KPI Cards
              _KpiRow(
                data: data,
                isArabic: isArabic,
                showRevenue: canViewRevenue,
              ),
              const SizedBox(height: 20),

              // Revenue Chart
              _RevenueChart(data: data.revenueChart, isArabic: isArabic),
              const SizedBox(height: 20),

              // Peak Hours Heatmap — Pro-only. FeatureGate shows an upgrade
              // CTA when the plan doesn't include advanced_analytics; the
              // provider already swallows the 403 so other tiles still render.
              FeatureGate(
                featureCode: FeatureCodes.advancedAnalytics,
                showLoadingShimmer: false,
                child:
                    _PeakHoursHeatmap(data: data.peakHours, isArabic: isArabic),
              ),
              const SizedBox(height: 20),

              // Top Classes
              _TopClassesList(classes: data.topClasses, isArabic: isArabic),
              const SizedBox(height: 20),

              // At Risk Members
              _AtRiskMembersList(
                members: data.atRiskMembers,
                isArabic: isArabic,
              ),
              const SizedBox(height: 20),

              // Coach of the Month (gated)
              FeatureGate(
                featureCode: FeatureCodes.coachLeaderboard,
                showLoadingShimmer: false,
                child: data.coachOfMonth != null
                    ? _CoachOfMonthCard(
                        coach: data.coachOfMonth!,
                        isArabic: isArabic,
                      )
                    : const SizedBox.shrink(),
              ),
              const SizedBox(height: 20),

              // Recent Activity Feed
              _RecentActivityFeed(
                activities: data.recentActivity,
                isArabic: isArabic,
              ),
              const SizedBox(height: 16),
            ],
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل البيانات' : 'Error loading data',
          onRetry: () => ref.invalidate(dashboardProvider),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// KPI Row
// ---------------------------------------------------------------------------
class _KpiRow extends StatelessWidget {
  final DashboardData data;
  final bool isArabic;

  /// Revenue is manager+ (docs/01 "View analytics/revenue").
  final bool showRevenue;

  const _KpiRow({
    required this.data,
    required this.isArabic,
    required this.showRevenue,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (showRevenue) ...[
          StatCard.gradient(
            label: isArabic ? 'ايراد الشهر' : 'Revenue this month',
            value: NumberFormatter.formatCurrency(data.revenueThisMonth),
            icon: Icons.trending_up_rounded,
            trend: data.revenueTrend,
            trendUp: true,
          ),
          const SizedBox(height: 12),
        ],
        Row(
          children: [
            Expanded(
              child: StatCard(
                label: isArabic ? 'الاعضاء النشطين' : 'Active Members',
                value: NumberFormatter.formatCompact(data.activeMembers),
                icon: Icons.people_alt_rounded,
                iconColor: AppColors.info,
                trend: data.memberTrend,
                trendUp: true,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: StatCard(
                label: isArabic ? 'حضور اليوم' : 'Check-ins today',
                value: data.todayCheckIns.toString(),
                icon: Icons.login_rounded,
                iconColor: AppColors.warning,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// Revenue Area Chart
// ---------------------------------------------------------------------------
class _RevenueChart extends StatelessWidget {
  final List<RevenueDataPoint> data;
  final bool isArabic;

  const _RevenueChart({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (data.isEmpty) return const SizedBox.shrink();

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'الايرادات' : 'Revenue',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 16),
          SizedBox(
            height: 200,
            child: LineChart(
              LineChartData(
                gridData: const FlGridData(show: false),
                titlesData: FlTitlesData(
                  leftTitles: const AxisTitles(
                    sideTitles: SideTitles(showTitles: false),
                  ),
                  rightTitles: const AxisTitles(
                    sideTitles: SideTitles(showTitles: false),
                  ),
                  topTitles: const AxisTitles(
                    sideTitles: SideTitles(showTitles: false),
                  ),
                  bottomTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 22,
                      getTitlesWidget: (value, meta) {
                        final index = value.toInt();
                        if (index >= 0 && index < data.length) {
                          return Text(
                            data[index].label,
                            style: theme.textTheme.labelSmall,
                          );
                        }
                        return const SizedBox.shrink();
                      },
                    ),
                  ),
                ),
                borderData: FlBorderData(show: false),
                lineBarsData: [
                  LineChartBarData(
                    spots: data
                        .asMap()
                        .entries
                        .map((e) => FlSpot(
                              e.key.toDouble(),
                              e.value.amount,
                            ))
                        .toList(),
                    isCurved: true,
                    color: theme.colorScheme.primary,
                    barWidth: 3,
                    isStrokeCapRound: true,
                    dotData: const FlDotData(show: false),
                    belowBarData: BarAreaData(
                      show: true,
                      color: theme.colorScheme.primary.withOpacity(0.1),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Peak Hours Heatmap
// ---------------------------------------------------------------------------
class _PeakHoursHeatmap extends StatelessWidget {
  final List<PeakHourEntry> data;
  final bool isArabic;

  const _PeakHoursHeatmap({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (data.isEmpty) return const SizedBox.shrink();

    final maxCount = data
        .map((e) => e.count)
        .reduce((a, b) => a > b ? a : b)
        .clamp(1, 9999);

    final dayLabels = isArabic
        ? ['ح', 'ن', 'ث', 'ر', 'خ', 'ج', 'س']
        : ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    // Group data by day and hour
    final Map<String, int> heatmapData = {};
    for (final entry in data) {
      heatmapData['${entry.dayOfWeek}_${entry.hour}'] = entry.count;
    }

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'اوقات الذروة' : 'Peak Hours',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 200,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Day labels
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const SizedBox(height: 20),
                      ...List.generate(7, (day) {
                        return SizedBox(
                          height: 22,
                          child: Center(
                            child: Text(
                              dayLabels[day],
                              style: theme.textTheme.labelSmall,
                            ),
                          ),
                        );
                      }),
                    ],
                  ),
                  const SizedBox(width: 4),
                  // Grid
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Hour headers (6am - 11pm)
                      Row(
                        children: List.generate(18, (h) {
                          final hour = h + 6;
                          return SizedBox(
                            width: 22,
                            height: 20,
                            child: Center(
                              child: Text(
                                '$hour',
                                style: theme.textTheme.labelSmall?.copyWith(
                                  fontSize: 8,
                                ),
                              ),
                            ),
                          );
                        }),
                      ),
                      // Data cells
                      ...List.generate(7, (day) {
                        return Row(
                          children: List.generate(18, (h) {
                            final hour = h + 6;
                            final count =
                                heatmapData['${day}_$hour'] ?? 0;
                            final intensity = count / maxCount;
                            return Container(
                              width: 20,
                              height: 20,
                              margin: const EdgeInsets.all(1),
                              decoration: BoxDecoration(
                                color: theme.colorScheme.primary
                                    .withOpacity(intensity.clamp(0.05, 1.0)),
                                borderRadius: BorderRadius.circular(3),
                              ),
                            );
                          }),
                        );
                      }),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Top Classes List
// ---------------------------------------------------------------------------
class _TopClassesList extends StatelessWidget {
  final List<TopClassEntry> classes;
  final bool isArabic;

  const _TopClassesList({required this.classes, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (classes.isEmpty) return const SizedBox.shrink();

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'افضل الحصص' : 'Top Classes',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 12),
          ...classes.take(5).map((c) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            c.className,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          Text(
                            c.instructorName,
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primary.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        '${c.bookings} ${_bookingsLabel(c.bookings, isArabic)}',
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: theme.colorScheme.primary,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              )),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// At Risk Members (10+ days no visit)
// ---------------------------------------------------------------------------
class _AtRiskMembersList extends StatelessWidget {
  final List<AtRiskMember> members;
  final bool isArabic;

  const _AtRiskMembersList({
    required this.members,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (members.isEmpty) return const SizedBox.shrink();

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.warning_amber_rounded,
                  color: AppColors.warning, size: 20),
              const SizedBox(width: 8),
              Text(
                isArabic ? 'اعضاء معرضون للخطر' : 'At-Risk Members',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            isArabic
                ? 'لم يزوروا منذ اكثر من 10 ايام'
                : 'Haven\'t visited in 10+ days',
            style: theme.textTheme.bodySmall,
          ),
          const SizedBox(height: 12),
          ...members.take(5).map((m) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    AvatarWidget(
                      imageUrl: m.photoUrl,
                      name: m.name,
                      size: AppConstants.avatarSizeSmall,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(m.name,
                              style: theme.textTheme.bodyMedium?.copyWith(
                                  fontWeight: FontWeight.w500)),
                          if (m.planName != null)
                            Text(m.planName!,
                                style: theme.textTheme.bodySmall),
                        ],
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppColors.error.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        '${m.daysSinceLastVisit} ${isArabic ? 'يوم' : 'days'}',
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: AppColors.error,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              )),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Coach of the Month
// ---------------------------------------------------------------------------
class _CoachOfMonthCard extends StatelessWidget {
  final CoachOfMonthData coach;
  final bool isArabic;

  const _CoachOfMonthCard({required this.coach, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return AppCard(
      border: Border.all(color: AppColors.gold, width: 2),
      child: Row(
        children: [
          Stack(
            children: [
              AvatarWidget(
                imageUrl: coach.trainerPhotoUrl,
                name: coach.trainerName,
                size: 56,
              ),
              Positioned(
                right: -2,
                bottom: -2,
                child: Container(
                  padding: const EdgeInsets.all(2),
                  decoration: const BoxDecoration(
                    color: AppColors.gold,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.emoji_events,
                    size: 16,
                    color: Colors.white,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  isArabic ? 'مدرب الشهر' : 'Coach of the Month',
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: AppColors.gold,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  coach.trainerName,
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.star, size: 14, color: AppColors.star),
                    const SizedBox(width: 4),
                    Text(
                      coach.averageRating.toStringAsFixed(1),
                      style: theme.textTheme.bodySmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Icon(Icons.fitness_center,
                        size: 14, color: theme.colorScheme.primary),
                    const SizedBox(width: 4),
                    Text(
                      '${coach.sessionsCompleted} ${isArabic ? 'حصة' : 'sessions'}',
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Recent Activity Feed
// ---------------------------------------------------------------------------
class _RecentActivityFeed extends StatelessWidget {
  final List<ActivityEntry> activities;
  final bool isArabic;

  const _RecentActivityFeed({
    required this.activities,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (activities.isEmpty) return const SizedBox.shrink();

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'النشاط الاخير' : 'Recent Activity',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 12),
          ...activities.take(10).map((a) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _activityIcon(a.type, theme),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(a.description,
                              style: theme.textTheme.bodyMedium),
                          const SizedBox(height: 2),
                          Text(
                            DateFormatter.formatRelative(a.timestamp,
                                locale: isArabic ? 'ar' : 'en'),
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              )),
        ],
      ),
    );
  }

  Widget _activityIcon(String type, ThemeData theme) {
    IconData icon;
    Color color;
    switch (type) {
      case 'check_in':
        icon = Icons.login;
        color = AppColors.success;
        break;
      case 'payment':
        icon = Icons.payment;
        color = AppColors.info;
        break;
      case 'new_member':
        icon = Icons.person_add;
        color = theme.colorScheme.primary;
        break;
      case 'class_booked':
        icon = Icons.event_available;
        color = AppColors.warning;
        break;
      default:
        icon = Icons.circle;
        color = AppColors.grey400;
    }
    return Container(
      padding: const EdgeInsets.all(6),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        shape: BoxShape.circle,
      ),
      child: Icon(icon, size: 16, color: color),
    );
  }
}

/// Arabic plural rules for "booking" noun after a count.
String _bookingsLabel(int count, bool isArabic) {
  if (!isArabic) return count == 1 ? 'booking' : 'bookings';
  if (count == 1) return 'حجز';
  if (count == 2) return 'حجزان';
  if (count >= 3 && count <= 10) return 'حجوزات';
  return 'حجز';
}
