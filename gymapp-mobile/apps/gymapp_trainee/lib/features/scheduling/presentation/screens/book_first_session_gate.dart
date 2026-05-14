import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';

/// Wraps app shell. Gate only triggers when the trainee has been *assigned*
/// session capacity (an Active package with remaining sessions) AND has not
/// yet booked any session. Without an active package the gate does nothing —
/// the trainee can use the app normally.
class BookFirstSessionGate extends ConsumerWidget {
  final Widget child;
  const BookFirstSessionGate({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider).valueOrNull;
    final memberships = auth?.user?.gymMemberships ?? const [];
    final gymId = auth?.currentGymId;

    final hasTrainer = memberships.any((m) =>
        (gymId == null || m.gymId == gymId) && m.assignedTrainerId != null);

    if (!hasTrainer) return child;

    final packagesAsync = ref.watch(activePackagesProvider);
    final packages = packagesAsync.valueOrNull;
    if (packages == null) return child;
    final hasActiveCapacity = packages.any((p) =>
        p.status.toLowerCase() == 'active' && p.remainingSessions > 0);
    if (!hasActiveCapacity) return child;

    final upcoming = ref.watch(upcomingSessionsListProvider).valueOrNull;
    final all = ref.watch(mySessionsAllProvider).valueOrNull;
    final hasAny = (upcoming != null && upcoming.isNotEmpty) ||
        (all != null && all.isNotEmpty);

    if (hasAny) return child;
    if (upcoming == null || all == null) return child;

    return const _GateScreen();
  }
}

class _GateScreen extends StatelessWidget {
  const _GateScreen();

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Icon(Icons.event_available,
                  size: 72,
                  color: Theme.of(context).colorScheme.primary),
              const SizedBox(height: 16),
              Text(
                isArabic ? 'احجز جلستك الأولى' : 'Book your first session',
                textAlign: TextAlign.center,
                style: Theme.of(context)
                    .textTheme
                    .headlineSmall
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 8),
              Text(
                isArabic
                    ? 'حدد موعد جلستك الأولى مع المدرب لتفعيل باقي ميزات التطبيق.'
                    : 'Pick a slot with your trainer. The rest of the app unlocks once your first session is booked.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                icon: const Icon(Icons.calendar_today),
                label: Text(isArabic ? 'اختر موعداً' : 'Pick a time'),
                onPressed: () => context.push(AppRoutes.bookSession),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
