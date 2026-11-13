import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../providers/pt_providers.dart';
import 'pt_dashboard_card.dart';

/// Dashboard for PT gyms (analytics off): what the desk does every day — assign a
/// trial or package, follow up finished trials, review disputed sessions — and the
/// owner's PT numbers.
class PtDeskHome extends ConsumerWidget {
  const PtDeskHome({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ar = Localizations.localeOf(context).languageCode == 'ar';
    final flags = ref.watch(featureFlagsProvider);
    final auth = ref.watch(authProvider).valueOrNull;
    final canAssign =
        auth?.hasPermission(StaffPermission.assignPackage) ?? false;
    final role = (auth?.staffRole ?? '').toLowerCase();
    final isOwnerOrManager = role == 'owner' || role == 'manager';
    final pagePad = context.responsivePagePadding;

    final trials = flags.isEnabled(FeatureFlagKeys.ptTrialEndAlerts)
        ? ref.watch(notConvertedTrialsProvider)
        : null;
    final interests = flags.isEnabled(FeatureFlagKeys.ptPackageInterest)
        ? ref.watch(pendingInterestsProvider)
        : null;
    final disputed = flags.isEnabled(FeatureFlagKeys.ptDualSessionLogging)
        ? ref.watch(disputedSessionsProvider)
        : null;

    return Scaffold(
      appBar: AppBar(title: Text(ar ? 'الرئيسية' : 'Home')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(notConvertedTrialsProvider);
          ref.invalidate(pendingInterestsProvider);
          ref.invalidate(disputedSessionsProvider);
          ref.invalidate(ptDashboardProvider(30));
        },
        child: ListView(
          padding: EdgeInsets.all(pagePad),
          children: [
            if (canAssign && flags.isEnabled(FeatureFlagKeys.ptPackages))
              _ActionTile(
                icon: Icons.person_add_alt_1,
                title: ar ? 'تعيين تجربة أو باقة' : 'Assign a trial or package',
                subtitle: ar
                    ? 'اختر المتدرب ثم «تعيين مدرب»'
                    : 'Pick the trainee, then “Assign trainer”',
                onTap: () => context.go('/members'),
              ),
            // Hottest leads first: trainees who asked for a package.
            if (interests != null)
              _ActionTile(
                icon: Icons.thumb_up_alt_outlined,
                title: ar ? 'مهتمون بالباقات' : 'Interested in a package',
                subtitle: ar
                    ? 'ضغطوا «مهتم» — تواصل معهم الآن'
                    : 'They tapped “I’m interested” — contact them now',
                count: interests.valueOrNull?.length,
                onTap: () => context.push('/pt/interests'),
              ),
            if (trials != null)
              _ActionTile(
                icon: Icons.hourglass_bottom,
                title: ar
                    ? 'أنهوا التجربة ولم يشتركوا'
                    : 'Finished trial, not converted',
                subtitle: ar
                    ? 'تواصل معهم لعرض الباقات'
                    : 'Follow up and offer a package',
                count: trials.valueOrNull?.length,
                onTap: () => context.push('/pt/trials'),
              ),
            if (disputed != null)
              _ActionTile(
                icon: Icons.fact_check_outlined,
                title: ar ? 'حصص للمراجعة' : 'Sessions to review',
                subtitle: ar
                    ? 'حصص قال المتدرب إنها لم تتم'
                    : 'Sessions a trainee says did not happen',
                count: disputed.valueOrNull?.length,
                onTap: () => context.push('/pt/review'),
              ),
            if (isOwnerOrManager &&
                flags.isEnabled(FeatureFlagKeys.ptOwnerDashboard)) ...[
              const SizedBox(height: 16),
              const PtDashboardCard(),
            ],
          ],
        ),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final int? count;
  final VoidCallback onTap;

  const _ActionTile(
      {required this.icon,
      required this.title,
      required this.subtitle,
      required this.onTap,
      this.count});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: theme.colorScheme.primary.withValues(alpha: 0.12),
          child: Icon(icon, color: theme.colorScheme.primary),
        ),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(subtitle),
        trailing: count == null || count == 0
            ? const Icon(Icons.chevron_right)
            : Badge(
                label: Text('$count'), child: const Icon(Icons.chevron_right)),
        onTap: onTap,
      ),
    );
  }
}
