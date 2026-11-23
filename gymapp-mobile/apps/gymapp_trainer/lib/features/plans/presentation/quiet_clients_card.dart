import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import '../data/coach_overview.dart';
import 'client_plans_strings.dart';

/// Home: "3 clients need a nudge" when clients with a plan went quiet; nothing otherwise.
class QuietClientsCard extends ConsumerWidget {
  const QuietClientsCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = ClientPlansStrings(Localizations.localeOf(context).languageCode == 'ar');
    final overview = ref.watch(coachPlanOverviewProvider).valueOrNull;
    if (overview == null || overview.quiet.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(top: 16),
      child: AppCard(
        key: const ValueKey('quiet-clients-card'),
        onTap: () => context.push(AppRoutes.clientPlans),
        child: Row(children: [
          const Icon(Icons.notifications_active_outlined, color: AppColors.warning),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(s.needsNudgeCount(overview), style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
              Text(s.clientPlansSubtitle, style: Theme.of(context).textTheme.bodySmall),
            ]),
          ),
          Icon(Directionality.of(context) == TextDirection.rtl ? Icons.chevron_left : Icons.chevron_right),
        ]),
      ),
    );
  }
}
