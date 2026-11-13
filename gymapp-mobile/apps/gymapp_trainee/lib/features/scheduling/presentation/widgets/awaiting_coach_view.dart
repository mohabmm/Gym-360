import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';

/// Shown instead of any sessions UI until the gym desk assigns this member a coach
/// and a number of sessions (the staff app is the only place that does this).
class AwaitingCoachView extends ConsumerWidget {
  const AwaitingCoachView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(activePackagesProvider);
        ref.invalidate(mySessionsAllProvider);
      },
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 64),
          EmptyState(
            icon: Icons.person_search_outlined,
            title: isArabic ? 'لم يتم تعيين مدرب بعد' : 'No coach assigned yet',
            subtitle: isArabic
                ? 'سيقوم الجيم بتعيين مدربك وعدد حصصك قريبًا، وستظهر حصصك هنا.'
                : 'Your gym will assign your coach and sessions soon. Your sessions will show here.',
          ),
        ],
      ),
    );
  }
}

/// Builds [child] only once a coach and sessions are assigned; otherwise the
/// [AwaitingCoachView]. Shows a spinner while that is being checked.
class CoachAssignedGate extends ConsumerWidget {
  final Widget child;

  const CoachAssignedGate({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final assigned = ref.watch(hasCoachAssignedProvider);
    if (assigned == true) return child;
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'حصصي' : 'My sessions')),
      body: assigned == null ? const Center(child: CircularProgressIndicator()) : const AwaitingCoachView(),
    );
  }
}
