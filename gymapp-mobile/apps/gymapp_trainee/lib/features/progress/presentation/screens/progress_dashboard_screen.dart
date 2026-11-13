import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/progress_providers.dart';

/// pt.progress_measurements: the member's progress since day one (coach measurements are the
/// baseline), plus the streak / check-in / photo sections when those features are on.
class ProgressDashboardScreen extends ConsumerWidget {
  const ProgressDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final flags = ref.watch(featureFlagsProvider);
    final showStreaks = flags.isEnabled(FeatureFlagKeys.gamificationStreaks);
    final showCheckIns = flags.isEnabled(FeatureFlagKeys.gymAttendance);
    final showPhotos = flags.isEnabled(FeatureFlagKeys.progressPhotos);

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
          refreshProgress(ref);
          if (showPhotos) ref.invalidate(progressPhotosProvider);
          await ref.read(progressJourneyProvider.future);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (showStreaks || showCheckIns || showPhotos)
              _ExtrasSection(
                showStreaks: showStreaks,
                showCheckIns: showCheckIns,
                showPhotos: showPhotos,
                isArabic: isArabic,
              ),
            const _JourneySection(),
            const SizedBox(height: 12),
            _QuickActions(showPhotos: showPhotos, isArabic: isArabic),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

/// Day one → now: hero, chart, comparison and check-ins (shared with the coach app).
class _JourneySection extends ConsumerWidget {
  const _JourneySection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final coach = ref.watch(assignedCoachProvider);
    return ref.watch(progressJourneyProvider).when(
          data: (journey) => ProgressJourneyView(
            journey: journey,
            isArabic: isArabic,
            viewer: MemberViewer(coachId: coach.id, coachName: coach.name),
            onEditGoal: () => _editGoal(context, ref, journey, isArabic),
          ),
          loading: () => const Column(children: [ShimmerCard(height: 160), ShimmerCard(height: 220)]),
          error: (_, __) => AppErrorWidget(
            message: "Couldn't load your progress",
            messageAr: 'تعذر تحميل تقدمك',
            onRetry: () => ref.invalidate(progressJourneyProvider),
          ),
        );
  }

  Future<void> _editGoal(BuildContext context, WidgetRef ref, ProgressJourney journey, bool isArabic) async {
    final selection = await showGoalPicker(
      context,
      strings: ProgressJourneyStrings(isArabic),
      current: journey.goal,
    );
    final gymId = ref.read(authProvider).valueOrNull?.currentGymId;
    if (selection == null || gymId == null) return;
    try {
      await ref.read(progressJourneyApiProvider).setGoal(GymId(gymId), selection);
      refreshProgress(ref);
    } on ApiException catch (_) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'تعذر حفظ الهدف، حاول مرة أخرى' : "Couldn't save the goal. Try again."),
      ));
    }
  }
}

/// Streak card and check-in / photo counters (each behind its own flag).
class _ExtrasSection extends ConsumerWidget {
  final bool showStreaks;
  final bool showCheckIns;
  final bool showPhotos;
  final bool isArabic;

  const _ExtrasSection({
    required this.showStreaks,
    required this.showCheckIns,
    required this.showPhotos,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(progressDashboardDetailProvider).when(
          data: (dashboard) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Column(
              children: [
                if (showStreaks)
                  _StreakCard(
                    currentStreak: dashboard.currentWorkoutStreak,
                    longestStreak: dashboard.longestWorkoutStreak,
                    isArabic: isArabic,
                  ),
                if (showCheckIns || showPhotos) ...[
                  if (showStreaks) const SizedBox(height: 8),
                  _Counters(dashboard: dashboard, showCheckIns: showCheckIns, showPhotos: showPhotos, isArabic: isArabic),
                ],
              ],
            ),
          ),
          loading: () => const ShimmerCard(height: 80),
          error: (_, __) => const SizedBox.shrink(),
        );
  }
}

class _Counters extends StatelessWidget {
  final ProgressDashboardModel dashboard;
  final bool showCheckIns;
  final bool showPhotos;
  final bool isArabic;

  const _Counters({
    required this.dashboard,
    required this.showCheckIns,
    required this.showPhotos,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        if (showCheckIns)
          Expanded(
            child: StatCard(
              label: isArabic ? 'أيام الحضور' : 'Check-In Streak',
              value: '${dashboard.currentCheckInStreak}',
              icon: Icons.event_available,
              iconColor: Colors.green,
              compact: true,
            ),
          ),
        if (showCheckIns && showPhotos) const SizedBox(width: 8),
        if (showPhotos)
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
  }
}

class _QuickActions extends StatelessWidget {
  final bool showPhotos;
  final bool isArabic;

  const _QuickActions({required this.showPhotos, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _ActionCard(
            icon: Icons.straighten,
            label: isArabic ? 'سجّل قياساتك' : 'Log measurements',
            onTap: () => context.push(AppRoutes.logMeasurements),
          ),
        ),
        if (showPhotos) const SizedBox(width: 12),
        if (showPhotos)
          Expanded(
            child: _ActionCard(
              icon: Icons.photo_camera,
              label: isArabic ? 'صور التقدم' : 'Progress Photos',
              onTap: () => context.push(AppRoutes.progressPhotos),
            ),
          ),
      ],
    );
  }
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
                  size: 28, color: Theme.of(context).colorScheme.primary),
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
