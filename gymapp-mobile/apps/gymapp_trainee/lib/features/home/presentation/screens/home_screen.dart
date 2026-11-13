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
import '../../../scheduling/presentation/providers/scheduling_providers.dart';
import '../../../scheduling/presentation/widgets/pt_session_widgets.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../../../progress/presentation/providers/progress_providers.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;
    final pagePad = context.responsivePagePadding;
    final flags = ref.watch(featureFlagsProvider);

    List<Widget> section(double pad, String flag, Widget child, {double top = 0}) =>
        flags.isEnabled(flag)
            ? [SliverToBoxAdapter(child: Padding(padding: EdgeInsets.fromLTRB(pad, top, pad, 8), child: child))]
            : const [];

    return RtlScaffold(
      isArabic: isArabic,
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(mySessionsAllProvider);
          ref.invalidate(activePackagesProvider);
          ref.invalidate(todaysWorkoutProvider);
          ref.invalidate(upcomingSessionsListProvider);
          ref.invalidate(progressDashboardProvider);
          ref.invalidate(todaysWaterProvider);
          ref.invalidate(progressJourneyProvider);
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
            // Every section belongs to a feature; disabled ones are not built at all,
            // so a PT gym sees packages, sessions to confirm and progress, with no gaps.
            ...section(pagePad, FeatureFlagKeys.ptPackages, const PackageSummaryCard(), top: 4),
            ...section(pagePad, FeatureFlagKeys.ptDualSessionLogging, const ConfirmSessionsCard()),
            ...section(pagePad, FeatureFlagKeys.ptProgressMeasurements, const _SinceDayOneSection()),
            ...section(pagePad, FeatureFlagKeys.trainingWorkouts, const TodaysWorkoutCard(), top: 4),
            ...section(pagePad, FeatureFlagKeys.schedulingBooking, const NextSessionCard()),
            SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(pagePad, 8, pagePad, 4),
                child: _StatsRow(),
              ),
            ),
            ...section(pagePad, FeatureFlagKeys.trainingWorkouts, const WeeklyActivityChart(), top: 12),
            if (flags.isEnabled(FeatureFlagKeys.communicationAnnouncements))
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
    final canCheckIn = ref.watch(isFeatureEnabledProvider(FeatureFlagKeys.gymAttendance));

    Widget wrap(Widget child) {
      if (!canCheckIn) return child;
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
    final flags = ref.watch(featureFlagsProvider);
    final cards = <Widget>[
      if (flags.isEnabled(FeatureFlagKeys.gamificationStreaks)) const _StreakStat(),
      if (flags.isEnabled(FeatureFlagKeys.nutrition)) const _WaterStat(),
    ];
    if (cards.isEmpty) return const SizedBox.shrink();

    return Row(
      children: [
        for (var i = 0; i < cards.length; i++) ...[
          if (i > 0) const SizedBox(width: 10),
          Expanded(child: cards[i]),
        ],
      ],
    );
  }
}

class _StreakStat extends ConsumerWidget {
  const _StreakStat();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final label = isArabic ? 'السلسلة' : 'Streak';
    return ref.watch(progressDashboardProvider).when(
          data: (progress) => StatCard.gradient(
            label: label,
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
            label: label,
            value: '-',
            icon: Icons.local_fire_department,
            iconColor: Colors.orange,
          ),
        );
  }
}

/// pt.progress_measurements: "Since day one" change; renders nothing until there are two check-ins.
class _SinceDayOneSection extends ConsumerWidget {
  const _SinceDayOneSection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return ref.watch(progressJourneyProvider).maybeWhen(
          data: (journey) => SinceDayOneCard(
            journey: journey,
            isArabic: isArabic,
            onTap: () => context.go(AppRoutes.progress),
          ),
          orElse: () => const SizedBox.shrink(),
        );
  }
}

class _WaterStat extends ConsumerWidget {
  const _WaterStat();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final label = isArabic ? 'الماء' : 'Water';
    return ref.watch(todaysWaterProvider).when(
          data: (water) => StatCard(
            label: label,
            value: '${(water.totalMl / 1000).toStringAsFixed(1)}L',
            icon: Icons.water_drop_outlined,
            iconColor: Colors.cyan,
            onTap: () => context.push(AppRoutes.waterTracker),
          ),
          loading: () => const ShimmerStatCard(),
          error: (_, __) => StatCard(
            label: label,
            value: '-',
            icon: Icons.water_drop_outlined,
            iconColor: Colors.cyan,
            onTap: () => context.push(AppRoutes.waterTracker),
          ),
        );
  }
}
