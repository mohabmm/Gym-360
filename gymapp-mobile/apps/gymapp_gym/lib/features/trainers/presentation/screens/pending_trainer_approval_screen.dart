import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/trainers_provider.dart';
import '../../../../core/error_text.dart';

class PendingTrainerApprovalScreen extends ConsumerWidget {
  const PendingTrainerApprovalScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(pendingTrainersProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'طلبات انضمام المدربين' : 'Pending Trainer Requests'),
      ),
      body: pending.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.check_circle_outline,
              title: isArabic ? 'لا توجد طلبات' : 'No pending requests',
              subtitle: isArabic ? 'جميع الطلبات تمت معالجتها' : 'All requests have been processed',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(pendingTrainersProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final t = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: Row(
                    children: [
                      AvatarWidget(
                        imageUrl: t.photoUrl,
                        name: '${t.firstName} ${t.lastName}',
                        size: 48,
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              '${t.firstName} ${t.lastName}',
                              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                    fontWeight: FontWeight.w600,
                                  ),
                            ),
                            if (t.phone != null) Text(t.phone!),
                          ],
                        ),
                      ),
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          IconButton(
                            icon: const Icon(Icons.check_circle, color: AppColors.success),
                            onPressed: () => _approve(context, ref, t.id, isArabic),
                          ),
                          IconButton(
                            icon: const Icon(Icons.cancel, color: AppColors.error),
                            onPressed: () => _reject(context, ref, t.id, isArabic),
                          ),
                        ],
                      ),
                    ],
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading requests',
          onRetry: () => ref.invalidate(pendingTrainersProvider),
        ),
      ),
    );
  }

  Future<void> _approve(
      BuildContext context, WidgetRef ref, String trainerId, bool isArabic) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      // migrated to currentGymIdProvider
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.post<void>(
        '${ApiEndpoints.gymTrainers(gymId)}/$trainerId/approve',
        parser: (_) {},
      );
      ref.invalidate(pendingTrainersProvider);
      ref.invalidate(trainersProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    }
  }

  Future<void> _reject(
      BuildContext context, WidgetRef ref, String trainerId, bool isArabic) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      // migrated to currentGymIdProvider
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.post<void>(
        '${ApiEndpoints.gymTrainers(gymId)}/$trainerId/reject',
        parser: (_) {},
      );
      ref.invalidate(pendingTrainersProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    }
  }
}
