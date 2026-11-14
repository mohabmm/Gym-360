import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../home/presentation/providers/trainer_dashboard_provider.dart';
import '../../../revenue/presentation/providers/revenue_providers.dart';
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

/// One tap logs a session that just happened (pt.dual_session_logging). The
/// snackbar offers the optional second tap: record measurements.
class QuickLogButton extends ConsumerStatefulWidget {
  final String memberId;
  final bool compact;
  const QuickLogButton({super.key, required this.memberId, this.compact = false});

  @override
  ConsumerState<QuickLogButton> createState() => _QuickLogButtonState();
}

class _QuickLogButtonState extends ConsumerState<QuickLogButton> {
  bool _busy = false;

  Future<void> _log() async {
    final isArabic = _isArabic(context);
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final canMeasure = ref.read(isFeatureEnabledProvider(FeatureFlagKeys.ptProgressMeasurements));
    final canLogExercises = ref.read(isFeatureEnabledProvider(FeatureFlagKeys.ptExerciseProgress));
    setState(() => _busy = true);
    try {
      final result = await ref.read(apiClientProvider).post<Map<String, dynamic>>(
            ApiEndpoints.sessionQuickLog,
            data: {'memberId': widget.memberId},
            parser: (data) => Map<String, dynamic>.from(data as Map),
          );
      final left = (result['remainingSessions'] as num?)?.toInt() ?? 0;
      final already = result['alreadyLogged'] == true;
      final sessionId = result['sessionId']?.toString() ?? '';
      ref.invalidate(allClientPackagesProvider);
      ref.invalidate(trainerDashboardProvider);
      messenger.showSnackBar(SnackBar(
        content: Text(already
            ? (isArabic ? 'تم تسجيل هذه الحصة بالفعل' : 'This session was already logged')
            : (isArabic ? 'تم تسجيل الحصة · متبقي $left' : 'Session logged · $left left')),
        action: already ? null : _nextStep(router, isArabic, sessionId, canLogExercises, canMeasure),
      ));
    } on FeatureDisabledException {
      // Flags refresh hides the button.
    } on ApiException catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(_friendly(e, isArabic))));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  /// The optional second tap: record the session's exercises (pt.exercise_progress), else measurements.
  SnackBarAction? _nextStep(GoRouter router, bool isArabic, String sessionId, bool canLogExercises, bool canMeasure) {
    if (canLogExercises && sessionId.isNotEmpty) {
      return SnackBarAction(
        label: isArabic ? 'التمارين' : 'Exercises',
        onPressed: () => router.push(AppRoutes.sessionExercisesFor(sessionId, widget.memberId)),
      );
    }
    if (!canMeasure) return null;
    return SnackBarAction(
      label: isArabic ? 'القياسات' : 'Measurements',
      onPressed: () => router.push(measurementsRoute(widget.memberId)),
    );
  }

  static String _friendly(ApiException e, bool isArabic) {
    final code = e.errorCode ?? '';
    if (code.contains('NoSessionsRemaining')) {
      return isArabic ? 'لا توجد حصص متبقية في الباقة' : 'No sessions left in the package';
    }
    if (code.contains('ClientNotAssignedToTrainer')) {
      return isArabic ? 'هذا العميل ليس لديه باقة معك' : 'This client has no package with you';
    }
    return isArabic ? 'تعذّر تسجيل الحصة، حاول مرة أخرى' : 'Could not log the session, please try again';
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final label = Text(isArabic ? 'سجّل حصة' : 'Log session');
    final icon = _busy
        ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
        : const Icon(Icons.check_circle_outline, size: 18);
    return widget.compact
        ? FilledButton.tonalIcon(onPressed: _busy ? null : _log, icon: icon, label: label)
        : FilledButton.icon(onPressed: _busy ? null : _log, icon: icon, label: label);
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
