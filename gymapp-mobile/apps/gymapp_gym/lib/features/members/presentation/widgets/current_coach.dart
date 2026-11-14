import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// The member's package that still has sessions left (and has not expired), or null.
/// While one exists the desk cannot assign another coach — the backend enforces the same.
SessionPackageModel? currentCoachPackage(List<SessionPackageModel> packages, {DateTime? now}) {
  final at = now ?? DateTime.now();
  for (final p in packages) {
    final active = p.status.toLowerCase() == 'active' && p.remainingSessions > 0;
    final expired = p.expiresAt != null && !p.expiresAt!.isAfter(at);
    if (active && !expired) return p;
  }
  return null;
}

/// The member's current coach package (null when they have none).
final memberCurrentCoachProvider =
    FutureProvider.autoDispose.family<SessionPackageModel?, String>((ref, memberId) async {
  final packages = await ref.watch(apiClientProvider).get<List<SessionPackageModel>>(
        ApiEndpoints.sessionPackages,
        queryParameters: {'memberId': memberId, 'status': 'Active'},
        parser: (data) => (data as List)
            .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
  return currentCoachPackage(packages);
});

/// Shown on the member profile in place of "Assign Trainer" while the member has a coach.
class CurrentCoachCard extends StatelessWidget {
  final SessionPackageModel package;
  final bool isArabic;

  const CurrentCoachCard({super.key, required this.package, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final coach = (package.trainerName ?? '').trim();
    final left = package.remainingSessions;
    return AppCard(
      child: Row(
        children: [
          const Icon(Icons.fitness_center),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  isArabic ? 'المدرب الحالي: $coach' : 'Current coach: $coach',
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 4),
                Text(
                  isArabic
                      ? 'متبقي $left حصة. يمكن تعيين مدرب جديد بعد انتهاء الحصص.'
                      : '$left sessions left. A new coach can be assigned when they are used.',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
