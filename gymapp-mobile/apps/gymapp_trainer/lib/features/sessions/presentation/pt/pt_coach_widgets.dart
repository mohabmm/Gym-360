import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../home/presentation/providers/trainer_dashboard_provider.dart';
import '../../../plans/data/plan_api.dart';
import '../../../plans/data/session_plan_days.dart';
import '../../../revenue/presentation/providers/revenue_providers.dart';
import 'record_session/record_session_sheet.dart';
import 'record_session/record_session_strings.dart';
import 'record_session/session_recorded_sheet.dart';
import 'record_session/session_time_draft.dart';
import 'trial_recap_widgets.dart';

bool _isArabic(BuildContext context) => Localizations.localeOf(context).languageCode == 'ar';

/// The coach's active client packages (trial and paid), most sessions left first.
final activeClientPackagesProvider = Provider.autoDispose<AsyncValue<List<SessionPackageModel>>>((ref) {
  return ref.watch(allClientPackagesProvider).whenData((packages) => packages
      .where((p) => p.status.toLowerCase() == 'active')
      .toList()
    ..sort((a, b) => a.remainingSessions.compareTo(b.remainingSessions)));
});

/// Route of the coach's "record measurements" screen for a client.
String measurementsRoute(String clientId) => '/clients/$clientId/measurements';

/// Opens "Record session" (pt.dual_session_logging): the coach confirms the client, says when the
/// session happened (today by default, up to 7 days back) and records it; then a sheet offers the
/// next steps — exercises (pt.exercise_progress) and measurements (pt.progress_measurements).
class QuickLogButton extends ConsumerStatefulWidget {
  final String memberId;
  final bool compact;
  const QuickLogButton({super.key, required this.memberId, this.compact = false});

  @override
  ConsumerState<QuickLogButton> createState() => _QuickLogButtonState();
}

class _QuickLogButtonState extends ConsumerState<QuickLogButton> {
  SessionPackageModel? get _package => ref
      .read(activeClientPackagesProvider)
      .valueOrNull
      ?.where((p) => p.memberId == widget.memberId && p.remainingSessions > 0)
      .firstOrNull;

  String get _clientName {
    final fromPackage = _package?.memberName ?? '';
    return fromPackage.isNotEmpty ? fromPackage : (ref.read(clientNameByIdProvider)[widget.memberId] ?? '');
  }

  static Future<RecordedSession> _send(ApiClient api, String memberId, Map<String, dynamic> payload) async {
    final result = await api.post<Map<String, dynamic>>(
          ApiEndpoints.sessionQuickLog,
          data: {'memberId': memberId, ...payload},
          parser: (data) => Map<String, dynamic>.from(data as Map),
        );
    return RecordedSession.fromJson(result);
  }

  Future<void> _open() async {
    final strings = RecordSessionStrings(_isArabic(context));
    final router = GoRouter.of(context);
    final draft = SessionTimeDraft(ref.read(clockProvider)());
    final name = _clientName;
    // Read everything now: the sheet can outlive this button (the list behind it reloads, e.g.
    // after a token refresh), and `ref` of an unmounted widget throws "No ProviderScope found".
    final package = _package;
    final planDays = ref.read(sessionPlanDaysProvider(widget.memberId)).valueOrNull;
    final api = ref.read(apiClientProvider);
    final memberId = widget.memberId;
    final container = ProviderScope.containerOf(context, listen: false);
    final result = await showModalBottomSheet<RecordedSession>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => RecordSessionSheet(
        clientName: name,
        package: package,
        strings: strings,
        draft: draft,
        onSubmit: (payload) => _send(api, memberId, payload),
        planDays: planDays,
      ),
    );
    if (result == null) return;
    // The session counts even if this button is gone: refresh the counts either way.
    container
      ..invalidate(allClientPackagesProvider)
      ..invalidate(traineePlansProvider(memberId))
      ..invalidate(sessionPlanDaysProvider(memberId))
      ..invalidate(trainerDashboardProvider);
    if (!mounted) return;
    final next = _nextSteps(router, result);
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (_) => SessionRecordedSheet(
        result: result,
        clientName: name,
        strings: strings,
        draft: draft,
        next: next,
      ),
    );
  }

  NextSteps _nextSteps(GoRouter router, RecordedSession result) {
    final canExercises = ref.read(isFeatureEnabledProvider(FeatureFlagKeys.ptExerciseProgress)) &&
        result.sessionId.isNotEmpty;
    final canMeasure = ref.read(isFeatureEnabledProvider(FeatureFlagKeys.ptProgressMeasurements));
    return NextSteps(
      exercises: canExercises
          ? () => router.push(AppRoutes.sessionExercisesFor(result.sessionId, widget.memberId))
          : null,
      measurements: canMeasure ? () => router.push(measurementsRoute(widget.memberId)) : null,
    );
  }

  @override
  Widget build(BuildContext context) {
    // Keep the client's package and name loaded so the sheet opens with them.
    ref.watch(activeClientPackagesProvider);
    ref.watch(clientNameByIdProvider);
    ref.watch(sessionPlanDaysProvider(widget.memberId));
    final label = Text(_isArabic(context) ? 'سجّل حصة' : 'Log session');
    const icon = Icon(Icons.check_circle_outline, size: 18);
    return widget.compact
        ? FilledButton.tonalIcon(key: const ValueKey('log-session'), onPressed: _open, icon: icon, label: label)
        : FilledButton.icon(key: const ValueKey('log-session'), onPressed: _open, icon: icon, label: label);
  }
}

/// Home panel: every active client package with sessions left, trial badge and
/// a one-tap "Log session" button.
class PtClientsPanel extends ConsumerWidget {
  const PtClientsPanel({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = _isArabic(context);
    final packagesAsync = ref.watch(activeClientPackagesProvider);
    final names = ref.watch(clientNameByIdProvider);

    return packagesAsync.when(
      data: (packages) {
        if (packages.isEmpty) {
          return EmptyState(
            icon: Icons.people_outline,
            title: isArabic ? 'لا يوجد عملاء نشطون' : 'No active clients',
            subtitle: isArabic ? 'يعيّن الاستقبال العملاء لك بباقة أو حصة تجريبية.' : 'The desk assigns clients to you with a package or a trial.',
          );
        }
        return Column(
          children: [
            for (final p in packages)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: ClientPackageTile(package: p, name: p.memberName ?? names[p.memberId] ?? ''),
              ),
          ],
        );
      },
      loading: () => const ShimmerCard(height: 120),
      error: (_, __) => const SizedBox.shrink(),
    );
  }
}

class ClientPackageTile extends StatelessWidget {
  final SessionPackageModel package;
  final String name;
  const ClientPackageTile({super.key, required this.package, required this.name});

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);
    final left = package.remainingSessions;
    final lowColor = left <= 1 ? AppColors.error : (left <= 3 ? AppColors.warning : null);

    return AppCard(
      onTap: () => context.push('/clients/${package.memberId}'),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(children: [
                  Flexible(
                    child: Text(name.isEmpty ? (isArabic ? 'عميل' : 'Client') : name,
                        style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700),
                        overflow: TextOverflow.ellipsis),
                  ),
                  if (package.isTrial) ...[
                    const SizedBox(width: 6),
                    TrialBadge(isArabic: isArabic),
                  ],
                ]),
                const SizedBox(height: 4),
                Text(
                  isArabic ? 'متبقي $left من ${package.totalSessions}' : '$left of ${package.totalSessions} left',
                  style: theme.textTheme.bodySmall?.copyWith(color: lowColor, fontWeight: lowColor == null ? null : FontWeight.w600),
                ),
              ],
            ),
          ),
          // Last free session: the coach can already send the plan.
          if (package.isTrial && left <= 1) SendPlanButton(package: package, clientName: name),
          if (left > 0)
            FeatureFlagGate(
              flag: FeatureFlagKeys.ptDualSessionLogging,
              child: QuickLogButton(memberId: package.memberId, compact: true),
            ),
        ],
      ),
    );
  }
}

class TrialBadge extends StatelessWidget {
  final bool isArabic;
  const TrialBadge({super.key, required this.isArabic});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
        decoration: BoxDecoration(
          color: AppColors.info.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Text(isArabic ? 'تجربة' : 'Trial',
            style: const TextStyle(color: AppColors.info, fontSize: 11, fontWeight: FontWeight.w600)),
      );
}
