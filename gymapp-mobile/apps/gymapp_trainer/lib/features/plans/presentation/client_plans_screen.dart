import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../clients/presentation/providers/client_providers.dart';
import '../data/coach_overview.dart';
import 'client_plans_strings.dart';

/// Clients by id from the coach's roster (name and phone for WhatsApp).
final _rosterByIdProvider = Provider.autoDispose<Map<String, UserModel>>((ref) =>
    ref.watch(clientRosterProvider).maybeWhen(
          data: (clients) => {for (final c in clients) c.user.id: c.user},
          orElse: () => const {},
        ));

/// pt.training_plan, part D: every client's plan at a glance — "needs a nudge" first (no workout
/// for the gym's inactive days), each with a push reminder and a WhatsApp message.
class ClientPlansScreen extends ConsumerWidget {
  const ClientPlansScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = ClientPlansStrings(Localizations.localeOf(context).languageCode == 'ar');
    return RtlScaffold(
      isArabic: s.isArabic,
      appBar: AppBar(title: Text(s.clientPlans)),
      body: ref.watch(coachPlanOverviewProvider).when(
            data: (overview) => RefreshIndicator(
              onRefresh: () => ref.refresh(coachPlanOverviewProvider.future),
              child: _OverviewList(overview: overview, s: s),
            ),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (_, __) => AppErrorWidget(
              message: s.loadFailed,
              onRetry: () => ref.invalidate(coachPlanOverviewProvider),
            ),
          ),
    );
  }
}

class _OverviewList extends StatelessWidget {
  final CoachPlanOverview overview;
  final ClientPlansStrings s;

  const _OverviewList({required this.overview, required this.s});

  @override
  Widget build(BuildContext context) {
    final quiet = overview.quiet;
    final onTrack = overview.clients.where((c) => c.hasPlan && !c.isInactive).toList();
    final noPlan = overview.clients.where((c) => !c.hasPlan).toList();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        SectionHeader(title: s.needsNudge, subtitle: s.quietRule(overview), icon: Icons.notifications_active_outlined),
        if (quiet.isEmpty) Padding(padding: const EdgeInsets.symmetric(vertical: 8), child: Text(s.allOnTrack)),
        for (final c in quiet) ClientPlanTile(client: c, s: s),
        if (onTrack.isNotEmpty) ...[
          const SizedBox(height: 16),
          SectionHeader(title: s.onTrack, icon: Icons.trending_up_rounded),
          for (final c in onTrack) ClientPlanTile(client: c, s: s),
        ],
        if (noPlan.isNotEmpty) ...[
          const SizedBox(height: 16),
          SectionHeader(title: s.noPlanYet, icon: Icons.assignment_outlined),
          for (final c in noPlan) ClientPlanTile(client: c, s: s),
        ],
      ],
    );
  }
}

/// One client: plan, this week, last workout; remind (push) and WhatsApp for quiet clients.
class ClientPlanTile extends ConsumerWidget {
  final CoachClientPlan client;
  final ClientPlansStrings s;

  const ClientPlanTile({super.key, required this.client, required this.s});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(_rosterByIdProvider)[client.traineeId];
    final name = s.name(user);
    return AppCard(
      key: ValueKey('client-plan-${client.traineeId}'),
      margin: const EdgeInsets.only(bottom: 8),
      onTap: () => context.push('/clients/${client.traineeId}'),
      child: Row(
        children: [
          Expanded(child: _Summary(client: client, name: name, s: s)),
          if (client.hasPlan) _Actions(client: client, user: user, s: s),
        ],
      ),
    );
  }
}

class _Summary extends StatelessWidget {
  final CoachClientPlan client;
  final String name;
  final ClientPlansStrings s;

  const _Summary({required this.client, required this.name, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final quiet = client.isInactive;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(name, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
        if (client.hasPlan) ...[
          Text(client.planDisplayName(s.isArabic) ?? '', style: theme.textTheme.bodySmall),
          Text(s.progress(client), style: theme.textTheme.bodySmall),
          Text(
            s.activity(client),
            style: theme.textTheme.bodySmall?.copyWith(
              color: quiet ? AppColors.warning : null,
              fontWeight: quiet ? FontWeight.w700 : null,
            ),
          ),
        ] else
          Text(s.noPlanHint, style: theme.textTheme.bodySmall),
      ],
    );
  }
}

class _Actions extends ConsumerStatefulWidget {
  final CoachClientPlan client;
  final UserModel? user;
  final ClientPlansStrings s;

  const _Actions({required this.client, required this.user, required this.s});

  @override
  ConsumerState<_Actions> createState() => _ActionsState();
}

class _ActionsState extends ConsumerState<_Actions> {
  bool _sending = false;

  ClientPlansStrings get s => widget.s;

  @override
  Widget build(BuildContext context) {
    final link = _whatsApp();
    return Row(mainAxisSize: MainAxisSize.min, children: [
      IconButton(
        key: ValueKey('remind-${widget.client.traineeId}'),
        tooltip: s.remind,
        onPressed: _sending ? null : _remind,
        icon: const Icon(Icons.notifications_active_outlined),
      ),
      if (link != null)
        IconButton(
          key: ValueKey('whatsapp-${widget.client.traineeId}'),
          tooltip: s.whatsApp,
          onPressed: () => launchUrl(link, mode: LaunchMode.externalApplication),
          icon: const Icon(Icons.chat_rounded, color: Color(0xFF25D366)),
        ),
    ]);
  }

  Uri? _whatsApp() => whatsAppLink(widget.user?.phone, s.whatsAppText(widget.user, widget.client));

  Future<void> _remind() async {
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _sending = true);
    String text;
    try {
      await ref.read(planNudgeProvider)(widget.client.traineeId);
      text = s.reminded;
    } on ApiException catch (e) {
      text = e.statusCode == 409 ? s.alreadyReminded : s.remindFailed;
    }
    if (!mounted) return;
    setState(() => _sending = false);
    messenger.showSnackBar(SnackBar(content: Text(text)));
  }
}
