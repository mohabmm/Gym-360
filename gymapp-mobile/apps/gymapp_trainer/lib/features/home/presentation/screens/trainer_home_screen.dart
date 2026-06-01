import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../communication/presentation/widgets/notification_bell.dart';
import '../providers/trainer_dashboard_provider.dart';

class TrainerHomeScreen extends ConsumerWidget {
  const TrainerHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final auth = ref.watch(authProvider);
    final dashboard = ref.watch(trainerDashboardProvider);
    final firstName = auth.user?.firstName ?? '';
    final lastName = auth.user?.lastName ?? '';
    final fullName = '$firstName $lastName'.trim();
    final pagePad = context.responsivePagePadding;
    final todayLabel = DateFormat(
      isArabic ? 'EEEE, d MMMM' : 'EEEE, MMMM d',
      isArabic ? 'ar' : 'en',
    ).format(DateTime.now());

    return RtlScaffold(
      isArabic: isArabic,
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(trainerDashboardProvider);
        },
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverAppBar(
              pinned: false,
              floating: true,
              snap: true,
              backgroundColor: theme.scaffoldBackgroundColor,
              surfaceTintColor: Colors.transparent,
              elevation: 0,
              title: Text(
                isArabic
                    ? '${AppConstants.appNameAr} - المدرب'
                    : '${AppConstants.appName} Trainer',
                style: theme.textTheme.titleLarge,
              ),
              actions: [
                IconButton(
                  icon: const Icon(Icons.chat_bubble_outline_rounded),
                  tooltip: isArabic ? 'الرسائل' : 'Messages',
                  onPressed: () => context.push(AppRoutes.chatList),
                ),
                const NotificationBell(),
                const SizedBox(width: 4),
              ],
            ),
            SliverPadding(
              padding: EdgeInsets.fromLTRB(pagePad, 8, pagePad, 24),
              sliver: SliverList(
                delegate: SliverChildListDelegate([
                  _GreetingHero(
                    fullName: fullName.isEmpty ? firstName : fullName,
                    isArabic: isArabic,
                    todayLabel: todayLabel,
                  ),
                  const SizedBox(height: 16),
                  dashboard.when(
                    data: (data) =>
                        _StatsRow(data: data, isArabic: isArabic),
                    loading: () => const _StatsSkeleton(),
                    error: (_, __) => const SizedBox.shrink(),
                  ),
                  const SizedBox(height: 24),
                  SectionHeader(
                    title:
                        isArabic ? 'جدول اليوم' : "Today's Schedule",
                    actionText: isArabic ? 'عرض الكل' : 'View All',
                    onAction: () => context.go(AppRoutes.schedule),
                  ),
                  dashboard.when(
                    data: (data) {
                      if (data.todaySessions.isEmpty) {
                        return EmptyState(
                          icon: Icons.event_available,
                          title: isArabic
                              ? 'لا توجد جلسات اليوم'
                              : 'No sessions today',
                          subtitle: isArabic
                              ? 'استمتع بيوم اجازتك!'
                              : 'Enjoy your day off!',
                        );
                      }
                      final names = ref.watch(clientNameByIdProvider);
                      return Column(
                        children: data.todaySessions
                            .map((session) => Padding(
                                  padding: const EdgeInsets.only(bottom: 8),
                                  child: _SessionCard(
                                    session: session,
                                    isArabic: isArabic,
                                    memberName: names[session.memberId],
                                  ),
                                ))
                            .toList(),
                      );
                    },
                    loading: () => Column(
                      children: List.generate(
                        3,
                        (_) => const Padding(
                          padding: EdgeInsets.only(bottom: 8),
                          child: ShimmerCard(height: 80),
                        ),
                      ),
                    ),
                    error: (e, _) => AppErrorWidget(
                      message: e.toString(),
                      onRetry: () =>
                          ref.invalidate(trainerDashboardProvider),
                    ),
                  ),
                  const SizedBox(height: 24),
                  SectionHeader(
                    title:
                        isArabic ? 'اجراءات سريعة' : 'Quick Actions',
                  ),
                  const SizedBox(height: 8),
                  _QuickActionsGrid(isArabic: isArabic),
                  const SizedBox(height: 24),
                  dashboard.when(
                    data: (data) =>
                        _MonthSummary(data: data, isArabic: isArabic),
                    loading: () => const ShimmerCard(height: 140),
                    error: (_, __) => const SizedBox.shrink(),
                  ),
                ]),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _GreetingHero extends StatelessWidget {
  final String fullName;
  final bool isArabic;
  final String todayLabel;

  const _GreetingHero({
    required this.fullName,
    required this.isArabic,
    required this.todayLabel,
  });

  @override
  Widget build(BuildContext context) {
    final greeting = _greeting(isArabic);
    final welcome = isArabic
        ? 'كابتن $fullName'
        : 'Coach $fullName';

    return AppCard.gradient(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 20),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  todayLabel,
                  style: const TextStyle(
                    color: Colors.white70,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.4,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  greeting,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 15,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  welcome,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    height: 1.2,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Container(
            width: 54,
            height: 54,
            decoration: BoxDecoration(
              color: Colors.white.withOpacity(0.2),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.fitness_center,
              color: Colors.white,
              size: 26,
            ),
          ),
        ],
      ),
    );
  }

  String _greeting(bool ar) {
    final h = DateTime.now().hour;
    if (h < 12) return ar ? 'صباح الخير،' : 'Good morning,';
    if (h < 17) return ar ? 'مساء النور،' : 'Good afternoon,';
    return ar ? 'مساء الخير،' : 'Good evening,';
  }
}

class _StatsSkeleton extends StatelessWidget {
  const _StatsSkeleton();
  @override
  Widget build(BuildContext context) => const Row(
        children: [
          Expanded(child: ShimmerCard(height: 96)),
          SizedBox(width: 12),
          Expanded(child: ShimmerCard(height: 96)),
          SizedBox(width: 12),
          Expanded(child: ShimmerCard(height: 96)),
        ],
      );
}

class _StatsRow extends StatelessWidget {
  final TrainerDashboardData data;
  final bool isArabic;

  const _StatsRow({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: StatCard(
            label: isArabic ? 'حجوزات معلقة' : 'Pending',
            value: '${data.pendingBookings}',
            icon: Icons.pending_actions,
            iconColor: AppColors.warning,
            onTap: () => context.push(AppRoutes.bookingRequests),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: StatCard(
            label: isArabic ? 'رسائل' : 'Messages',
            value: '${data.unreadMessages}',
            icon: Icons.chat_bubble_outline,
            iconColor: AppColors.info,
            onTap: () => context.push(AppRoutes.chatList),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: StatCard(
            label: isArabic ? 'عملاء' : 'Clients',
            value: '${data.totalClients}',
            icon: Icons.people_outline,
            iconColor: AppColors.success,
            onTap: () => context.go(AppRoutes.clients),
          ),
        ),
      ],
    );
  }
}

class _SessionCard extends ConsumerWidget {
  final SessionModel session;
  final bool isArabic;
  final String? memberName;

  const _SessionCard({
    required this.session,
    required this.isArabic,
    this.memberName,
  });

  static bool _canStartSession(String status, {bool testMode = false}) {
    final s = status.toLowerCase();
    if (testMode && s == 'pending') return true;
    return s == 'confirmed' ||
        s == 'scheduled' ||
        s == 'inprogress' ||
        s == 'in_progress';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final timeStr = DateFormat('hh:mm a', isArabic ? 'ar' : 'en').format(session.scheduledAt);
    // Trigger BE testing-mode fetch so the local mirror stays fresh; the value
    // we evaluate against is still the synced [testModeProvider].
    ref.watch(serverTestingModeProvider);
    final testMode = ref.watch(testModeProvider);
    final canStart = _canStartSession(session.status, testMode: testMode);
    final isNext = session.scheduledAt.isAfter(DateTime.now()) && canStart;

    return AppCard(
      onTap: canStart
          ? () => context.push('/sessions/${session.id}/live')
          : null,
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Container(
            padding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: isNext
                  ? theme.colorScheme.primary.withOpacity(0.1)
                  : AppColors.grey100,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Column(
              children: [
                Text(
                  timeStr,
                  style: theme.textTheme.titleSmall?.copyWith(
                    color: isNext
                        ? theme.colorScheme.primary
                        : AppColors.grey600,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(
                  '${session.durationMinutes}m',
                  style: theme.textTheme.bodySmall,
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  (memberName != null && memberName!.isNotEmpty)
                      ? memberName!
                      : (isArabic ? 'عميل' : 'Client'),
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                Text(
                  session.sessionType,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color:
                        theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          if (isNext)
            Container(
              padding: const EdgeInsets.symmetric(
                  horizontal: 10, vertical: 5),
              decoration: BoxDecoration(
                gradient: AppColors.primaryGradient,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                isArabic ? 'التالي' : 'NEXT',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 11,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 0.4,
                ),
              ),
            )
          else
            _StatusChip(status: session.status, isArabic: isArabic),
        ],
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  final bool isArabic;

  const _StatusChip({required this.status, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    Color color;
    String label;
    switch (status.toLowerCase()) {
      case 'completed':
        color = AppColors.success;
        label = isArabic ? 'مكتمل' : 'Done';
        break;
      case 'cancelledbytrainer':
      case 'cancelledbymember':
      case 'latecancelled':
      case 'cancelled':
        color = AppColors.error;
        label = isArabic ? 'ملغي' : 'Cancelled';
        break;
      case 'noshow':
      case 'no_show':
        color = AppColors.grey500;
        label = isArabic ? 'لم يحضر' : 'No Show';
        break;
      case 'pending':
        color = AppColors.warning;
        label = isArabic ? 'معلق' : 'Pending';
        break;
      case 'inprogress':
      case 'in_progress':
        color = AppColors.info;
        label = isArabic ? 'جاري' : 'In Progress';
        break;
      default:
        color = AppColors.info;
        label = isArabic ? 'مجدول' : 'Scheduled';
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _QuickActionsGrid extends ConsumerWidget {
  final bool isArabic;

  const _QuickActionsGrid({required this.isArabic});

  /// Pick the best session to "start" right now:
  ///  1. An in-progress session (resume it)
  ///  2. The next confirmed session today
  /// Returns null if there's nothing to start — caller should fall back to
  /// the Schedule screen so the trainer can pick one manually.
  static SessionModel? _nextStartableSession(
    List<SessionModel> todaySessions, {
    bool testMode = false,
  }) {
    bool isScheduled(String s) {
      final l = s.toLowerCase();
      return l == 'confirmed' || l == 'scheduled' || (testMode && l == 'pending');
    }
    bool isInProgress(String s) {
      final l = s.toLowerCase();
      return l == 'inprogress' || l == 'in_progress';
    }
    final active = todaySessions.where((s) => isInProgress(s.status)).toList();
    if (active.isNotEmpty) {
      active.sort((a, b) => a.scheduledAt.compareTo(b.scheduledAt));
      return active.first;
    }
    final upcoming = todaySessions.where((s) => isScheduled(s.status)).toList()
      ..sort((a, b) => a.scheduledAt.compareTo(b.scheduledAt));
    return upcoming.isNotEmpty ? upcoming.first : null;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(trainerDashboardProvider);
    final testMode = ref.watch(testModeProvider);
    final todaySessions = dashboard.maybeWhen(
      data: (d) => d.todaySessions,
      orElse: () => const <SessionModel>[],
    );

    final actions = [
      _QuickAction(
        icon: Icons.play_circle_outline,
        label: isArabic ? 'بدء جلسة' : 'Start Session',
        onTap: () {
          final target = _nextStartableSession(todaySessions, testMode: testMode);
          if (target != null) {
            context.push('/sessions/${target.id}/live');
          } else {
            context.go(AppRoutes.schedule);
          }
        },
      ),
      _QuickAction(
        icon: Icons.fitness_center,
        label: isArabic ? 'بناء تمرين' : 'Build Workout',
        onTap: () => context.push(AppRoutes.workoutBuilder),
      ),
      _QuickAction(
        icon: Icons.calendar_month,
        label: isArabic ? 'الجدول' : 'Schedule',
        onTap: () => context.go(AppRoutes.schedule),
      ),
      _QuickAction(
        icon: Icons.chat_outlined,
        label: isArabic ? 'محادثة' : 'Chat',
        onTap: () => context.push(AppRoutes.chatList),
      ),
    ];

    final columns = context.gridColumns(phone: 4, tablet: 4, largeTablet: 6);
    return GridView.count(
      crossAxisCount: columns,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10,
      crossAxisSpacing: 10,
      childAspectRatio: 0.85,
      children: actions
          .map((a) => _QuickActionTile(action: a))
          .toList(),
    );
  }
}

class _QuickActionTile extends StatelessWidget {
  final _QuickAction action;
  const _QuickActionTile({required this.action});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    return AppCard.outlined(
      onTap: action.onTap,
      padding: const EdgeInsets.all(10),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [
                  theme.colorScheme.primary.withOpacity(isDark ? 0.25 : 0.14),
                  theme.colorScheme.primary.withOpacity(isDark ? 0.1 : 0.06),
                ],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              shape: BoxShape.circle,
            ),
            child: Icon(
              action.icon,
              color: theme.colorScheme.primary,
              size: 24,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            action.label,
            style: theme.textTheme.labelSmall?.copyWith(
              fontWeight: FontWeight.w600,
            ),
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

class _QuickAction {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _QuickAction({
    required this.icon,
    required this.label,
    required this.onTap,
  });
}

class _MonthSummary extends StatelessWidget {
  final TrainerDashboardData data;
  final bool isArabic;

  const _MonthSummary({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final monthName = DateFormat(
      'MMMM',
      isArabic ? 'ar' : 'en',
    ).format(DateTime.now());

    return AppCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  gradient: AppColors.goldGradient,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(Icons.insights_rounded,
                    color: Colors.white, size: 18),
              ),
              const SizedBox(width: 10),
              Text(
                isArabic ? 'ملخص $monthName' : '$monthName summary',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              _SummaryItem(
                value: '${data.sessionsCompletedThisMonth}',
                label: isArabic ? 'جلسات مكتملة' : 'Sessions Done',
                icon: Icons.check_circle_outline,
              ),
              const SizedBox(width: 16),
              _SummaryItem(
                value: '${data.newClientsThisMonth}',
                label: isArabic ? 'عملاء جدد' : 'New Clients',
                icon: Icons.person_add_outlined,
              ),
              const SizedBox(width: 16),
              _SummaryItem(
                value: data.averageRating > 0
                    ? data.averageRating.toStringAsFixed(1)
                    : '-',
                label: isArabic ? 'متوسط التقييم' : 'Avg Rating',
                icon: Icons.star_outline,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SummaryItem extends StatelessWidget {
  final String value;
  final String label;
  final IconData icon;

  const _SummaryItem({
    required this.value,
    required this.label,
    required this.icon,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Expanded(
      child: Column(
        children: [
          Icon(icon, color: theme.colorScheme.primary, size: 22),
          const SizedBox(height: 6),
          Text(
            value,
            style: theme.textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
          ),
          Text(
            label,
            style: theme.textTheme.bodySmall,
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}
