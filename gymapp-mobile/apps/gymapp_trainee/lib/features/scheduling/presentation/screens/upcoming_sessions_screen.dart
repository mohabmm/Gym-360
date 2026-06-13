import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';
import '../session_status_l10n.dart';
import 'class_schedule_screen.dart';

class UpcomingSessionsScreen extends ConsumerWidget {
  const UpcomingSessionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final bookable = ref.watch(bookableSessionsProvider);
    final canBook = (bookable ?? 0) > 0;

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(isArabic ? 'الجدول' : 'Schedule'),
          actions: [
            IconButton(
              icon: const Icon(Icons.add),
              tooltip: canBook
                  ? (isArabic ? 'احجز جلسة' : 'Book Session')
                  : (isArabic
                      ? 'بلغت الحد الأقصى للجلسات المحجوزة'
                      : 'Booking capacity reached'),
              onPressed:
                  canBook ? () => context.push(AppRoutes.bookSession) : null,
            ),
          ],
          bottom: TabBar(
            tabs: [
              Tab(
                icon: const Icon(Icons.event_note),
                text: isArabic ? 'جلساتي' : 'My Sessions',
              ),
              Tab(
                icon: const Icon(Icons.groups),
                text: isArabic ? 'الحصص' : 'Classes',
              ),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            _MySessionsView(),
            ClassScheduleView(),
          ],
        ),
      ),
    );
  }
}

class _MySessionsView extends ConsumerWidget {
  const _MySessionsView();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final sessionsAsync = ref.watch(upcomingSessionsListProvider);
    final allSessionsAsync = ref.watch(mySessionsAllProvider);
    final packagesAsync = ref.watch(activePackagesProvider);

    final packages = packagesAsync.valueOrNull ?? const <SessionPackageModel>[];
    final activePackages = packages
        .where((p) => p.status.toLowerCase() == 'active' && p.remainingSessions > 0)
        .toList();

    final now = DateTime.now();
    final pastSessions = (allSessionsAsync.valueOrNull ?? const <SessionModel>[])
        .where((s) => s.scheduledAt.isBefore(now))
        .toList()
      ..sort((a, b) => b.scheduledAt.compareTo(a.scheduledAt));

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(upcomingSessionsListProvider);
        ref.invalidate(mySessionsAllProvider);
        ref.invalidate(activePackagesProvider);
      },
      child: sessionsAsync.when(
        data: (sessions) {
          final children = <Widget>[];
          if (activePackages.isNotEmpty) {
            children.add(_PackagesSection(packages: activePackages, isArabic: isArabic));
          }
          if (sessions.isEmpty) {
            children.addAll([
              const SizedBox(height: 40),
              EmptyState(
                icon: Icons.calendar_today_outlined,
                title: isArabic
                    ? 'لا توجد جلسات قادمة'
                    : 'No upcoming sessions',
                subtitle: activePackages.isNotEmpty
                    ? (isArabic
                        ? 'احجز جلستك التالية من الباقة أعلاه'
                        : 'Book your next session from the package above')
                    : (isArabic
                        ? 'احجز جلستك الأولى مع المدرب'
                        : 'Book your first session with a trainer'),
                actionLabel: isArabic ? 'احجز الآن' : 'Book Now',
                onAction: () => context.push(AppRoutes.bookSession),
              ),
            ]);
          } else {
            children.addAll([
              Padding(
                padding: const EdgeInsetsDirectional.fromSTEB(16, 8, 16, 8),
                child: Text(
                  isArabic ? 'الجلسات القادمة' : 'Upcoming sessions',
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
              ),
              ...sessions.map((s) => _SessionTile(session: s, isArabic: isArabic)),
            ]);
          }
          if (pastSessions.isNotEmpty) {
            children.addAll([
              const SizedBox(height: 8),
              Padding(
                padding: const EdgeInsetsDirectional.fromSTEB(16, 8, 16, 8),
                child: Text(
                  isArabic ? 'الجلسات السابقة' : 'Past sessions',
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
              ),
              ...pastSessions.take(20).map(
                  (s) => _SessionTile(session: s, isArabic: isArabic)),
            ]);
          }
          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.symmetric(vertical: 8),
            children: children,
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic
              ? 'تعذر تحميل الجلسات. يرجى المحاولة مرة أخرى.'
              : 'Could not load sessions. Please try again.',
          onRetry: () => ref.invalidate(upcomingSessionsListProvider),
        ),
      ),
    );
  }
}

class _PackagesSection extends StatelessWidget {
  final List<SessionPackageModel> packages;
  final bool isArabic;

  const _PackagesSection({required this.packages, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsetsDirectional.fromSTEB(16, 8, 16, 8),
          child: Text(
            isArabic ? 'باقاتي مع المدرب' : 'My Trainer Packages',
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
        ...packages.map((p) => _PackageCard(package: p, isArabic: isArabic)),
      ],
    );
  }
}

class _PackageCard extends ConsumerWidget {
  final SessionPackageModel package;
  final bool isArabic;

  const _PackageCard({required this.package, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bookable = ref.watch(bookableSessionsProvider);
    final canBook = (bookable ?? 0) > 0;
    final used = package.totalSessions - package.remainingSessions;
    final progress = package.totalSessions > 0
        ? used / package.totalSessions
        : 0.0;
    final trainerName = (package.trainerName ?? '').trim().isEmpty
        ? (isArabic ? 'مدرب' : 'Trainer')
        : package.trainerName!;
    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.card_membership, size: 18),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    package.name ?? package.packageName ?? (isArabic ? 'باقة' : 'Package'),
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              '${isArabic ? 'المدرب: ' : 'Trainer: '}$trainerName',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: progress.clamp(0.0, 1.0),
                minHeight: 6,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              isArabic
                  ? '${package.remainingSessions} متبقية من ${package.totalSessions}'
                  : '${package.remainingSessions} of ${package.totalSessions} sessions left',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                icon: const Icon(Icons.add, size: 18),
                label: Text(canBook
                    ? (isArabic ? 'احجز جلسة' : 'Book Session')
                    : (isArabic
                        ? 'بلغت الحد الأقصى للجلسات المحجوزة'
                        : 'Booking capacity reached')),
                onPressed:
                    canBook ? () => context.push(AppRoutes.bookSession) : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SessionTile extends ConsumerWidget {
  final SessionModel session;
  final bool isArabic;

  const _SessionTile({required this.session, required this.isArabic});

  /// Resolve the trainer's display name by matching the session's [trainerId]
  /// against the current user's gym memberships (BE emits
  /// [GymMembershipModel.assignedTrainerName] per gym). Falls back to the
  /// generic "Trainer" label only when no match exists — fixes BUG-19 where
  /// the upcoming card always said "Trainer" instead of "Demo Trainer".
  String _trainerDisplayName(WidgetRef ref) {
    final memberships =
        ref.read(authProvider).valueOrNull?.user?.gymMemberships ?? const [];
    final match = memberships.firstWhere(
      (m) => m.assignedTrainerId == session.trainerId,
      orElse: () => const GymMembershipModel(gymId: ''),
    );
    final name = match.assignedTrainerName;
    if (name != null && name.trim().isNotEmpty) return name;
    return isArabic ? 'مدرب' : 'Trainer';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dateStr = DateFormat(isArabic ? 'EEEE، dd MMMM' : 'EEEE, MMM dd',
            isArabic ? 'ar' : 'en')
        .format(session.scheduledAt);
    final timeStr =
        DateFormat('HH:mm').format(session.scheduledAt);

    final statusColor = _statusColor(session.status);
    final trainerDisplay = _trainerDisplayName(ref);
    final windowHours = ref
            .watch(gymSessionPolicyProvider)
            .valueOrNull
            ?.cancellationWindowHours ??
        24;
    final canCancel = _canCancel(session.status) &&
        DateTime.now().isBefore(
            session.scheduledAt.subtract(Duration(hours: windowHours)));

    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppConstants.borderRadius),
        onTap: () => context.push(
            AppRoutes.sessionDetail.replaceFirst(':sessionId', session.id)),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      trainerDisplay,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: statusColor.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      _statusLabel(session.status, isArabic),
                      style: TextStyle(
                          color: statusColor,
                          fontSize: 12,
                          fontWeight: FontWeight.w600),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(Icons.calendar_today_outlined, size: 14),
                  const SizedBox(width: 4),
                  Text(dateStr,
                      style: Theme.of(context).textTheme.bodySmall),
                  const SizedBox(width: 12),
                  const Icon(Icons.access_time, size: 14),
                  const SizedBox(width: 4),
                  Text(timeStr,
                      style: Theme.of(context).textTheme.bodySmall),
                ],
              ),
              if (canCancel)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Align(
                    alignment: AlignmentDirectional.centerEnd,
                    child: TextButton.icon(
                      icon: const Icon(Icons.cancel_outlined, size: 18),
                      label: Text(
                        isArabic ? 'إلغاء الحجز' : 'Cancel Booking',
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      style: TextButton.styleFrom(
                        foregroundColor: Colors.red,
                      ),
                      onPressed: () => _confirmCancel(context, ref),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  bool _canCancel(String status) {
    final s = status.toLowerCase();
    return s == 'scheduled' || s == 'confirmed' || s == 'pending';
  }

  Future<void> _confirmCancel(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'إلغاء الحجز' : 'Cancel Session'),
        content: Text(isArabic
            ? 'هل تريد إلغاء هذه الجلسة؟'
            : 'Are you sure you want to cancel this session?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'تراجع' : 'Keep'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'إلغاء الحجز' : 'Cancel'),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      final userId = ref.read(authProvider).valueOrNull?.user?.id;
      if (userId == null) throw Exception('Not signed in');
      final repo = ref.read(schedulingRepositoryProvider);
      await repo.cancelSession(session.id, cancelledBy: userId);
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(upcomingSessionsProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic ? 'تم إلغاء الحجز' : 'Session cancelled'),
        ));
      }
    } catch (e) {
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(mySessionsAllProvider);
      if (context.mounted) {
        final msg = e.toString().contains('InvalidStatusTransition')
            ? (isArabic
                ? 'تعذر إلغاء الجلسة بسبب تغير حالتها'
                : 'Cannot cancel — session status has changed')
            : (isArabic
                ? 'تعذر إلغاء الجلسة. يرجى المحاولة مرة أخرى.'
                : 'Could not cancel the session. Please try again.');
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    }
  }

  String _statusLabel(String status, bool isArabic) =>
      sessionStatusLabel(status, isArabic);

  Color _statusColor(String status) => sessionStatusColor(status);
}
