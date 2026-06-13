import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/home_providers.dart';
import '../widgets/todays_workout_card.dart';
import '../widgets/next_session_card.dart';
import '../widgets/weekly_activity_chart.dart';
import '../../../communication/presentation/providers/communication_providers.dart';
import '../../../communication/presentation/widgets/notification_bell.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;
    final pagePad = context.responsivePagePadding;

    return RtlScaffold(
      isArabic: isArabic,
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(todaysWorkoutProvider);
          ref.invalidate(upcomingSessionsProvider);
          ref.invalidate(progressDashboardProvider);
          ref.invalidate(todaysWaterProvider);
        },
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 12, pagePad, 4),
                child: _GreetingRow(
                  user: user,
                  isArabic: isArabic,
                ),
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 4, pagePad, 8),
                child: const TodaysWorkoutCard(),
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 0, pagePad, 8),
                child: const NextSessionCard(),
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 8, pagePad, 4),
                child: _StatsRow(),
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 12, pagePad, 8),
                child: const WeeklyActivityChart(),
              ),
            ),
            SliverToBoxAdapter(
              child: _AnnouncementsFeed(
                isArabic: isArabic,
                pagePad: pagePad,
              ),
            ),
            const SliverToBoxAdapter(child: SizedBox(height: 24)),
          ],
        ),
      ),
    );
  }

}

class _GreetingRow extends ConsumerWidget {
  final UserModel? user;
  final bool isArabic;

  const _GreetingRow({
    required this.user,
    required this.isArabic,
  });

  String _greeting() {
    final hour = DateTime.now().hour;
    if (hour < 12) return isArabic ? 'صباح الخير' : 'Good morning';
    if (hour < 17) return isArabic ? 'مساء النور' : 'Good afternoon';
    return isArabic ? 'مساء الخير' : 'Good evening';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _greeting(),
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                user?.firstName ?? '',
                style: theme.textTheme.displaySmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
        const NotificationBell(),
        const SizedBox(width: 4),
        const _GymLogo(),
      ],
    );
  }
}

class _AnnouncementsFeed extends ConsumerWidget {
  final bool isArabic;
  final double pagePad;
  const _AnnouncementsFeed({required this.isArabic, required this.pagePad});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final announcementsAsync = ref.watch(announcementsProvider);

    return announcementsAsync.when(
      data: (announcements) {
        if (announcements.isEmpty) return const SizedBox.shrink();
        final recent = announcements.take(3).toList();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SectionHeader(
              title: isArabic ? 'إعلانات الصالة' : 'Gym Announcements',
              actionText: isArabic ? 'عرض الكل' : 'View All',
              onAction: () => context.go(AppRoutes.announcements),
              icon: Icons.campaign_rounded,
            ),
            ...recent.map(
              (a) => Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 2, pagePad, 6),
                child: AppListCard(
                  leading: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      gradient: AppColors.goldGradient,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(
                      Icons.campaign,
                      color: Colors.white,
                      size: 20,
                    ),
                  ),
                  title: isArabic && a.titleAr != null ? a.titleAr! : a.title,
                  subtitle: isArabic && a.contentAr != null
                      ? a.contentAr!
                      : a.content,
                  onTap: () => context.go(AppRoutes.announcements),
                ),
              ),
            ),
          ],
        );
      },
      loading: () => const SizedBox.shrink(),
      error: (_, __) => const SizedBox.shrink(),
    );
  }
}

class _GymLogo extends ConsumerWidget {
  const _GymLogo();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final branding = ref.watch(gymBrandingProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final semanticLabel = isArabic ? 'الدخول إلى النادي' : 'Check in';

    Widget wrap(Widget child) {
      return Semantics(
        button: true,
        label: semanticLabel,
        child: InkWell(
          borderRadius: BorderRadius.circular(12),
          onTap: () => context.push(AppRoutes.checkin),
          child: child,
        ),
      );
    }

    return branding.when(
      data: (b) {
        if (b?.logoUrl != null) {
          return wrap(
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: Image.network(
                b!.logoUrl!,
                width: 44,
                height: 44,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => _defaultLogo(context),
              ),
            ),
          );
        }
        return wrap(_defaultLogo(context));
      },
      loading: () => wrap(_defaultLogo(context)),
      error: (_, __) => wrap(_defaultLogo(context)),
    );
  }

  Widget _defaultLogo(BuildContext context) {
    return Container(
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        gradient: AppColors.primaryGradient,
        borderRadius: BorderRadius.circular(12),
        boxShadow: AppColors.primaryGlow(opacity: 0.2),
      ),
      child: const Icon(
        Icons.fitness_center,
        color: Colors.white,
        size: 22,
      ),
    );
  }
}

class _StatsRow extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final progressAsync = ref.watch(progressDashboardProvider);
    final waterAsync = ref.watch(todaysWaterProvider);

    return Row(
      children: [
        Expanded(
          child: progressAsync.when(
            data: (progress) => StatCard.gradient(
              label: isArabic ? 'السلسلة' : 'Streak',
              value: '${progress.currentWorkoutStreak}',
              icon: Icons.local_fire_department,
              gradientOverride: const LinearGradient(
                colors: [Color(0xFFFB923C), Color(0xFFEA580C)],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              onTap: () => context.push(AppRoutes.progress),
            ),
            loading: () => const ShimmerStatCard(),
            error: (_, __) => StatCard(
              label: isArabic ? 'السلسلة' : 'Streak',
              value: '-',
              icon: Icons.local_fire_department,
              iconColor: Colors.orange,
              onTap: () => context.push(AppRoutes.progress),
            ),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: progressAsync.when(
            data: (progress) {
              final weight = progress.latestMetrics?.weight;
              final change = progress.weightChangeFromFirst;
              return StatCard(
                label: isArabic ? 'الوزن' : 'Weight',
                value: weight != null
                    ? '${weight.toStringAsFixed(1)} kg'
                    : '-',
                icon: Icons.monitor_weight_outlined,
                iconColor: AppColors.info,
                trend: change != null
                    ? '${change > 0 ? '+' : ''}${change.toStringAsFixed(1)}'
                    : null,
                trendUp: (change ?? 0) > 0,
                onTap: () => context.push(AppRoutes.logWeight),
              );
            },
            loading: () => const ShimmerStatCard(),
            error: (_, __) => StatCard(
              label: isArabic ? 'الوزن' : 'Weight',
              value: '-',
              icon: Icons.monitor_weight_outlined,
              iconColor: AppColors.info,
              onTap: () => context.push(AppRoutes.logWeight),
            ),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: waterAsync.when(
            data: (water) => StatCard(
              label: isArabic ? 'الماء' : 'Water',
              value: '${(water.totalMl / 1000).toStringAsFixed(1)}L',
              icon: Icons.water_drop_outlined,
              iconColor: Colors.cyan,
              onTap: () => context.push(AppRoutes.waterTracker),
            ),
            loading: () => const ShimmerStatCard(),
            error: (_, __) => StatCard(
              label: isArabic ? 'الماء' : 'Water',
              value: '-',
              icon: Icons.water_drop_outlined,
              iconColor: Colors.cyan,
              onTap: () => context.push(AppRoutes.waterTracker),
            ),
          ),
        ),
      ],
    );
  }
}
