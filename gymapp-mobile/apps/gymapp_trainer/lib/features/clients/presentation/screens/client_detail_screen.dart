import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../../core/session_type_l10n.dart';
import '../../../communication/presentation/providers/communication_providers.dart';
import '../providers/client_providers.dart';

/// Client detail screen with 5 tabs: Overview, Progress, Sessions, Workouts, Notes.
class ClientDetailScreen extends ConsumerStatefulWidget {
  final String clientId;

  const ClientDetailScreen({super.key, required this.clientId});

  @override
  ConsumerState<ClientDetailScreen> createState() =>
      _ClientDetailScreenState();
}

class _ClientDetailScreenState extends ConsumerState<ClientDetailScreen>
    with TickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 5, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final clientAsync = ref.watch(clientDetailProvider(widget.clientId));

    return Scaffold(
      appBar: AppBar(
        title: clientAsync.whenOrNull(
          data: (data) => Text(
            '${data.user.firstName} ${data.user.lastName}',
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.chat_bubble_outline),
            tooltip: isArabic ? 'محادثة' : 'Chat',
            onPressed: () => _startChat(context, ref),
          ),
          IconButton(
            icon: const Icon(Icons.note_add_outlined),
            tooltip: isArabic ? 'ملاحظات' : 'Notes',
            onPressed: () =>
                context.push('/clients/${widget.clientId}/notes'),
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              switch (value) {
                case 'onboarding':
                  context.push(
                      '/clients/${widget.clientId}/onboarding');
                  break;
                case 'remote_workout':
                  _showRemoteWorkoutDialog(context, ref);
                  break;
                case 'assign_workout':
                  context.push('/clients/${widget.clientId}/assign-workout');
                  break;
              }
            },
            itemBuilder: (context) => [
              PopupMenuItem(
                value: 'onboarding',
                child: ListTile(
                  dense: true,
                  leading: const Icon(Icons.assignment_outlined),
                  title: Text(isArabic ? 'نموذج الاستقبال' : 'Intake Form'),
                ),
              ),
              PopupMenuItem(
                value: 'remote_workout',
                child: ListTile(
                  dense: true,
                  leading: const Icon(Icons.wifi_tethering),
                  title: Text(
                      isArabic ? 'التمرين عن بعد' : 'Remote Workout'),
                ),
              ),
              PopupMenuItem(
                value: 'assign_workout',
                child: ListTile(
                  dense: true,
                  leading: const Icon(Icons.fitness_center),
                  title: Text(isArabic ? 'تعيين تمرين' : 'Assign Workout'),
                ),
              ),
            ],
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          isScrollable: true,
          tabs: [
            Tab(text: isArabic ? 'نظرة عامة' : 'Overview'),
            Tab(text: isArabic ? 'التقدم' : 'Progress'),
            Tab(text: isArabic ? 'الجلسات' : 'Sessions'),
            Tab(text: isArabic ? 'التمارين' : 'Workouts'),
            Tab(text: isArabic ? 'ملاحظات' : 'Notes'),
          ],
        ),
      ),
      body: clientAsync.when(
        data: (data) => TabBarView(
          controller: _tabController,
          children: [
            _OverviewTab(data: data, isArabic: isArabic),
            _ProgressTab(data: data, isArabic: isArabic),
            _SessionsTab(data: data, isArabic: isArabic),
            _WorkoutsTab(data: data, isArabic: isArabic),
            _NotesTab(
              data: data,
              clientId: widget.clientId,
              isArabic: isArabic,
            ),
          ],
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: e.toString(),
          onRetry: () =>
              ref.invalidate(clientDetailProvider(widget.clientId)),
        ),
      ),
    );
  }

  Future<void> _startChat(BuildContext context, WidgetRef ref) async {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final gymId = ref.read(activeGymIdProvider);
    if (gymId == null) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(
            isArabic ? 'لا يوجد جيم نشط' : 'No active gym'),
      ));
      return;
    }
    try {
      final apiClient = ref.read(apiClientProvider);
      final conversationId = await apiClient.post<String>(
        ApiEndpoints.conversations,
        data: {
          'gymId': gymId,
          'participant2': widget.clientId,
        },
        parser: (data) => data.toString(),
      );
      ref.invalidate(conversationsProvider);
      if (!context.mounted) return;
      context.push('/more/chat/$conversationId');
    } catch (_) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذر بدء المحادثة. حاول مرة أخرى.'
            : 'Could not start chat. Try again.'),
      ));
    }
  }

  void _showRemoteWorkoutDialog(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final reasonController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => Consumer(
        builder: (ctx, ref, _) {
          final remoteStatus =
              ref.watch(remoteWorkoutProvider(widget.clientId));
          return AlertDialog(
            title: Text(
              isArabic ? 'التمرين عن بعد' : 'Remote Workout',
            ),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    isArabic
                        ? 'تفعيل/تعطيل التمرين عن بعد لهذا العميل'
                        : 'Enable/disable remote workouts for this client',
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    label: isArabic ? 'السبب (اختياري)' : 'Reason (optional)',
                    hint: isArabic
                        ? 'مثال: في اجازة حتى 20 ابريل'
                        : "e.g., I'm on leave until April 20",
                    controller: reasonController,
                    maxLines: 2,
                  ),
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: Text(isArabic ? 'الغاء' : 'Cancel'),
              ),
              remoteStatus.whenOrNull(
                    data: (status) {
                      final isActive = status?.isActive ?? false;
                      return ElevatedButton(
                        onPressed: () async {
                          final authState =
                              ref.read(authProvider);
                          final trainerId = authState.user?.id;
                          final gymId = ref.read(activeGymIdProvider);
                          if (trainerId == null || gymId == null) {
                            if (ctx.mounted) {
                              ScaffoldMessenger.of(ctx).showSnackBar(
                                const SnackBar(
                                  content: Text(
                                      'No authenticated trainer or active gym'),
                                ),
                              );
                            }
                            return;
                          }
                          final apiClient = ref.read(apiClientProvider);
                          if (isActive) {
                            await apiClient.postVoid(
                              ApiEndpoints.remoteWorkoutDeactivate,
                              data: {
                                'trainerId': trainerId,
                                'clientId': widget.clientId,
                                'gymId': gymId,
                              },
                            );
                          } else {
                            await apiClient.postVoid(
                              ApiEndpoints.remoteWorkoutActivate,
                              data: {
                                'trainerId': trainerId,
                                'clientId': widget.clientId,
                                'gymId': gymId,
                                if (reasonController.text.isNotEmpty)
                                  'reason': reasonController.text,
                              },
                            );
                          }
                          ref.invalidate(
                              remoteWorkoutProvider(widget.clientId));
                          ref.invalidate(
                              clientDetailProvider(widget.clientId));
                          if (ctx.mounted) Navigator.pop(ctx);
                        },
                        child: Text(isActive
                            ? (isArabic ? 'تعطيل' : 'Deactivate')
                            : (isArabic ? 'تفعيل' : 'Activate')),
                      );
                    },
                  ) ??
                  const SizedBox.shrink(),
            ],
          );
        },
      ),
    );
  }
}

class _OverviewTab extends StatelessWidget {
  final ClientDetailData data;
  final bool isArabic;

  const _OverviewTab({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Client info card
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  AvatarWidget(
                    imageUrl: data.user.photoUrl,
                    name:
                        '${data.user.firstName} ${data.user.lastName}',
                    size: 80,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    '${data.user.firstName} ${data.user.lastName}',
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  if (data.user.email.isNotEmpty)
                    Text(
                      data.user.email,
                      style: theme.textTheme.bodySmall,
                    ),
                  const SizedBox(height: 16),
                  // Info chips
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      if (data.user.fitnessLevel != null)
                        Chip(
                          label: Text(data.user.fitnessLevel!),
                          avatar: const Icon(Icons.fitness_center, size: 16),
                        ),
                      if (data.user.gender != null)
                        Chip(
                          label: Text(data.user.gender!),
                          avatar: const Icon(Icons.person, size: 16),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Goals
          if (data.user.goals != null && data.user.goals!.isNotEmpty) ...[
            Text(
              isArabic ? 'الاهداف' : 'Goals',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: data.user.goals!
                  .map((goal) => Chip(label: Text(goal)))
                  .toList(),
            ),
            const SizedBox(height: 16),
          ],

          // Health conditions
          if (data.user.healthConditions != null &&
              data.user.healthConditions!.isNotEmpty) ...[
            Text(
              isArabic ? 'الحالات الصحية' : 'Health Conditions',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: data.user.healthConditions!
                  .map((c) => Chip(
                        label: Text(c),
                        backgroundColor: AppColors.warning.withOpacity(0.1),
                      ))
                  .toList(),
            ),
            const SizedBox(height: 16),
          ],

          // Remote workout status
          if (data.remoteWorkoutStatus != null)
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: data.remoteWorkoutStatus!.isActive
                    ? AppColors.success.withOpacity(0.1)
                    : AppColors.grey100,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  Icon(
                    Icons.wifi_tethering,
                    color: data.remoteWorkoutStatus!.isActive
                        ? AppColors.success
                        : AppColors.grey400,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      data.remoteWorkoutStatus!.isActive
                          ? (isArabic
                              ? 'التمرين عن بعد مفعل'
                              : 'Remote Workout Active')
                          : (isArabic
                              ? 'التمرين عن بعد معطل'
                              : 'Remote Workout Inactive'),
                      style: theme.textTheme.bodyMedium,
                    ),
                  ),
                ],
              ),
            ),
          const SizedBox(height: 16),

          // Active package
          if (data.activePackage != null) ...[
            Text(
              isArabic ? 'الباقة الحالية' : 'Active Package',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 8),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    const Icon(Icons.inventory_2_outlined),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            // BE SessionPackageDto carries the label as `name`;
                            // `packageName` only exists on older payloads.
                            data.activePackage!.packageName ??
                                data.activePackage!.name ??
                                (isArabic ? 'باقة' : 'Package'),
                            style: theme.textTheme.titleSmall,
                          ),
                          Text(
                            '${data.activePackage!.remainingSessions}/${data.activePackage!.totalSessions} ${isArabic ? 'جلسات متبقية' : 'sessions remaining'}',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    if (data.activePackage!.remainingSessions <= 3)
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: data.activePackage!.remainingSessions == 1
                              ? AppColors.error.withOpacity(0.1)
                              : AppColors.warning.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          isArabic ? 'منخفض' : 'Low',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                            color:
                                data.activePackage!.remainingSessions == 1
                                    ? AppColors.error
                                    : AppColors.warning,
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _ProgressTab extends StatelessWidget {
  final ClientDetailData data;
  final bool isArabic;

  const _ProgressTab({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (data.bodyMetrics.isEmpty) {
      return EmptyState(
        icon: Icons.trending_up,
        title: isArabic
            ? 'لا توجد بيانات تقدم'
            : 'No progress data yet',
        subtitle: isArabic
            ? 'سيظهر تقدم العميل هنا'
            : "Client's progress will appear here",
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: data.bodyMetrics.length,
      itemBuilder: (context, index) {
        final metric = data.bodyMetrics[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: ListTile(
            leading: const Icon(Icons.monitor_weight_outlined),
            title: Text(
              '${(metric.weight ?? 0).toStringAsFixed(1)} ${metric.weightUnit ?? 'kg'}',
              style: theme.textTheme.titleSmall,
            ),
            subtitle: Text(
              DateFormat('dd/MM/yyyy').format(metric.createdAt),
            ),
          ),
        );
      },
    );
  }
}

class _SessionsTab extends StatelessWidget {
  final ClientDetailData data;
  final bool isArabic;

  const _SessionsTab({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (data.sessions.isEmpty) {
      return EmptyState(
        icon: Icons.event_outlined,
        title:
            isArabic ? 'لا توجد جلسات' : 'No sessions yet',
      );
    }

    final now = DateTime.now();
    final sorted = [...data.sessions]
      ..sort((a, b) => b.scheduledAt.compareTo(a.scheduledAt));
    final upcoming =
        sorted.where((s) => s.scheduledAt.isAfter(now)).toList().reversed.toList();
    final past = sorted.where((s) => !s.scheduledAt.isAfter(now)).toList();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (upcoming.isNotEmpty) ...[
          Text(isArabic ? 'القادمة' : 'Upcoming',
              style:
                  theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          ...upcoming.map((s) => _SessionRow(
              session: s, isArabic: isArabic, parent: this)),
          const SizedBox(height: 16),
        ],
        if (past.isNotEmpty) ...[
          Text(isArabic ? 'السابقة' : 'Past',
              style:
                  theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          ...past.map((s) =>
              _SessionRow(session: s, isArabic: isArabic, parent: this)),
        ],
      ],
    );
  }

  void showDetailSheet(BuildContext context, SessionModel session) {
    showModalBottomSheet(
      context: context,
      showDragHandle: true,
      builder: (ctx) {
        final theme = Theme.of(ctx);
        return Consumer(builder: (ctx, ref, _) {
        final testMode = ref.watch(testModeProvider);
        final lead = const Duration(minutes: 10);
        final canStartByTime = !DateTime.now()
            .isBefore(session.scheduledAt.subtract(lead));
        final s = session.status.toLowerCase();
        final canStart = (testMode || canStartByTime) &&
            (s == 'confirmed' ||
                s == 'inprogress' ||
                (testMode && s == 'pending'));
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  DateFormat('EEEE, MMM dd yyyy', isArabic ? 'ar' : 'en')
                      .format(session.scheduledAt),
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 4),
                Text(
                  '${DateFormat('HH:mm').format(session.scheduledAt)} • '
                  '${session.durationMinutes} ${isArabic ? 'دقيقة' : 'min'} • '
                  '${sessionTypeLabel(session.sessionType, isArabic)}',
                ),
                const SizedBox(height: 8),
                Text(
                  '${isArabic ? 'الحالة:' : 'Status:'} ${_statusLabel(session.status, isArabic)}',
                  style: TextStyle(color: _statusColor(session.status)),
                ),
                if ((session.focus ?? '').isNotEmpty) ...[
                  const SizedBox(height: 12),
                  Text(isArabic ? 'التركيز' : 'Focus',
                      style: theme.textTheme.labelMedium),
                  Text(session.focus!),
                ],
                const SizedBox(height: 16),
                if (canStart)
                  FilledButton.icon(
                    icon: const Icon(Icons.play_arrow),
                    label: Text(isArabic ? 'ابدأ الجلسة' : 'Start session'),
                    onPressed: () {
                      Navigator.pop(ctx);
                      context.push('/sessions/${session.id}/live');
                    },
                  )
                else if (session.status.toLowerCase() == 'confirmed' ||
                    session.status.toLowerCase() == 'inprogress')
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      isArabic
                          ? 'لا يمكن بدء الجلسة قبل موعدها'
                          : 'Session cannot start before its scheduled time',
                      style: theme.textTheme.bodySmall,
                    ),
                  ),
              ],
            ),
          ),
        );
        });
      },
    );
  }

  // BE SessionStatus enum (PascalCase): Pending, Confirmed, InProgress,
  // Completed, CancelledByTrainer, CancelledByMember, LateCancelled, NoShow.
  // Normalize to lowercase for switch matching so callers don't have to.
  String _normalizeStatus(String s) => s.toLowerCase();

  Color _statusColor(String status) {
    switch (_normalizeStatus(status)) {
      case 'completed':
        return AppColors.success;
      case 'cancelled':
      case 'cancelledbytrainer':
      case 'cancelledbymember':
      case 'latecancelled':
      case 'late_cancelled':
        return AppColors.error;
      case 'noshow':
      case 'no_show':
        return AppColors.grey500;
      default:
        return AppColors.info;
    }
  }

  IconData _statusIcon(String status) {
    switch (_normalizeStatus(status)) {
      case 'completed':
        return Icons.check_circle;
      case 'cancelled':
      case 'cancelledbytrainer':
      case 'cancelledbymember':
      case 'latecancelled':
      case 'late_cancelled':
        return Icons.cancel;
      case 'noshow':
      case 'no_show':
        return Icons.person_off;
      default:
        return Icons.schedule;
    }
  }

  String _statusLabel(String status, bool isArabic) {
    switch (_normalizeStatus(status)) {
      case 'completed':
        return isArabic ? 'مكتمل' : 'Completed';
      case 'cancelled':
      case 'cancelledbytrainer':
        return isArabic ? 'ملغي' : 'Cancelled';
      case 'cancelledbymember':
        return isArabic ? 'ألغاه المتدرب' : 'Cancelled by trainee';
      case 'latecancelled':
      case 'late_cancelled':
        return isArabic ? 'الغاء متاخر' : 'Late Cancel';
      case 'noshow':
      case 'no_show':
        return isArabic ? 'لم يحضر' : 'No Show';
      default:
        return isArabic ? 'مجدول' : 'Scheduled';
    }
  }
}

class _SessionRow extends StatelessWidget {
  final SessionModel session;
  final bool isArabic;
  final _SessionsTab parent;

  const _SessionRow({
    required this.session,
    required this.isArabic,
    required this.parent,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        onTap: () => parent.showDetailSheet(context, session),
        leading: CircleAvatar(
          backgroundColor:
              parent._statusColor(session.status).withOpacity(0.1),
          child: Icon(
            parent._statusIcon(session.status),
            color: parent._statusColor(session.status),
          ),
        ),
        title: Text(
          DateFormat('dd/MM/yyyy hh:mm a', isArabic ? 'ar' : 'en')
              .format(session.scheduledAt),
          style: theme.textTheme.titleSmall,
        ),
        subtitle: Text(
            '${session.durationMinutes} ${isArabic ? "دقيقة" : "min"} • ${sessionTypeLabel(session.sessionType, isArabic)}'),
        trailing: Text(
          parent._statusLabel(session.status, isArabic),
          style: TextStyle(
            color: parent._statusColor(session.status),
            fontWeight: FontWeight.w600,
            fontSize: 12,
          ),
        ),
      ),
    );
  }
}

class _WorkoutsTab extends StatelessWidget {
  final ClientDetailData data;
  final bool isArabic;

  const _WorkoutsTab({required this.data, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (data.workoutAssignments.isEmpty) {
      return EmptyState(
        icon: Icons.fitness_center,
        title: isArabic
            ? 'لا توجد تمارين معينة'
            : 'No workouts assigned',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: data.workoutAssignments.length,
      itemBuilder: (context, index) {
        final assignment = data.workoutAssignments[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: ListTile(
            leading: const CircleAvatar(
              child: Icon(Icons.fitness_center),
            ),
            title: Text(
              assignment.workout?.name ?? (isArabic ? 'تمرين' : 'Workout'),
              style: theme.textTheme.titleSmall,
            ),
            subtitle: Text(
              assignment.scheduledDate != null
                  ? DateFormat('dd/MM/yyyy')
                      .format(assignment.scheduledDate!)
                  : (assignment.status ?? ''),
            ),
            trailing: const Icon(Icons.chevron_right),
          ),
        );
      },
    );
  }
}

class _NotesTab extends ConsumerWidget {
  final ClientDetailData data;
  final String clientId;
  final bool isArabic;

  const _NotesTab({
    required this.data,
    required this.clientId,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    if (data.notes.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            EmptyState(
              icon: Icons.note_outlined,
              title: isArabic
                  ? 'لا توجد ملاحظات'
                  : 'No notes yet',
              subtitle: isArabic
                  ? 'اضف ملاحظات خاصة عن هذا العميل'
                  : 'Add private notes about this client',
            ),
            const SizedBox(height: 16),
            AppButton.outlined(
              text: isArabic ? 'اضافة ملاحظة' : 'Add Note',
              onPressed: () => context.push('/clients/$clientId/notes'),
              width: 200,
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: data.notes.length,
      itemBuilder: (context, index) {
        final note = data.notes[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  note.content,
                  style: theme.textTheme.bodyMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  DateFormat('dd/MM/yyyy hh:mm a', isArabic ? 'ar' : 'en')
                      .format(DateFormatter.apiUtcToLocal(note.createdAt)),
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: AppColors.grey400,
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
