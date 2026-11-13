import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';
import '../widgets/pt_session_widgets.dart';

/// "My sessions" for PT gyms (no self-booking): remaining sessions per package,
/// sessions to confirm, and the history of logged sessions.
class PtSessionsScreen extends ConsumerWidget {
  const PtSessionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final sessionsAsync = ref.watch(mySessionsAllProvider);
    final pagePad = context.responsivePagePadding;

    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'حصصي' : 'My sessions')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(mySessionsAllProvider);
          ref.invalidate(activePackagesProvider);
        },
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: EdgeInsets.fromLTRB(pagePad, 12, pagePad, 24),
          children: [
            const FeatureFlagGate(flag: FeatureFlagKeys.ptPackages, child: PackageSummaryCard()),
            const ConfirmSessionsCard(),
            const SizedBox(height: 12),
            Text(isArabic ? 'الحصص المسجلة' : 'Logged sessions',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            ...sessionsAsync.when(
              data: (sessions) => _history(context, sessions, isArabic),
              loading: () => const [Center(child: Padding(padding: EdgeInsets.all(24), child: CircularProgressIndicator()))],
              error: (_, __) => [
                AppErrorWidget(
                  message: isArabic ? 'تعذر تحميل الحصص.' : 'Could not load sessions.',
                  onRetry: () => ref.invalidate(mySessionsAllProvider),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  List<Widget> _history(BuildContext context, List<SessionModel> sessions, bool isArabic) {
    final logged = sessions.where((s) => s.status == 'Completed').toList()
      ..sort((a, b) => b.scheduledAt.compareTo(a.scheduledAt));
    if (logged.isEmpty) {
      return [
        EmptyState(
          icon: Icons.event_available_outlined,
          title: isArabic ? 'لا توجد حصص بعد' : 'No sessions yet',
          subtitle: isArabic ? 'سيظهر هنا كل ما يسجله مدربك من حصص.' : 'Every session your coach logs shows up here.',
        ),
      ];
    }
    final format = DateFormat('EEE d MMM yyyy · HH:mm', isArabic ? 'ar' : 'en');
    return [
      for (final s in logged.take(30))
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const CircleAvatar(child: Icon(Icons.fitness_center, size: 18)),
          title: Text(format.format(s.scheduledAt)),
          subtitle: s.focus == null ? null : Text(s.focus!, maxLines: 1, overflow: TextOverflow.ellipsis),
          trailing: Text(confirmationLabel(s.memberConfirmation, isArabic),
              style: Theme.of(context).textTheme.bodySmall),
          onTap: () => context.push('/schedule/sessions/${s.id}'),
        ),
    ];
  }
}
