import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/gamification_provider.dart';
import '../../../../core/error_text.dart';

class ModerateTestimonialsScreen extends ConsumerWidget {
  const ModerateTestimonialsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final testimonials = ref.watch(testimonialsProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'مراجعة آراء العملاء' : 'Moderate Testimonials'),
      ),
      body: testimonials.when(
        data: (pending) {
          if (pending.isEmpty) {
            return EmptyState(
              icon: Icons.rate_review_outlined,
              title: isArabic ? 'لا توجد آراء بانتظار المراجعة' : 'No pending testimonials',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(testimonialsProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: pending.length,
              itemBuilder: (context, index) {
                final t = pending[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(
                            Icons.rate_review_outlined,
                            size: 16,
                            color: AppColors.warning,
                          ),
                          const Spacer(),
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: AppColors.warning.withOpacity(0.15),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              isArabic ? 'معلق' : 'Pending',
                              style: const TextStyle(
                                color: AppColors.warning,
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(t.content, style: theme.textTheme.bodyMedium),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: AppButton.outlined(
                              text: isArabic ? 'رفض' : 'Reject',
                              backgroundColor: AppColors.error,
                              onPressed: () => _moderate(
                                  context, ref, t, false, isArabic),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: AppButton(
                              text: isArabic ? 'موافقة' : 'Approve',
                              backgroundColor: AppColors.success,
                              onPressed: () => _moderate(
                                  context, ref, t, true, isArabic),
                            ),
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
        error: (e, _) {
          // This feature is Pro-gated; the backend returns
          // PLAN.FEATURE_NOT_AVAILABLE. Showing a generic "load error + retry"
          // is misleading (retry can't enable the feature) — show an upgrade
          // message instead, matching the dashboard's gating handling.
          if (e is ApiException &&
              e.errorCode == 'PLAN.FEATURE_NOT_AVAILABLE') {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  isArabic
                      ? 'مراجعة آراء العملاء متاحة في باقة Pro فقط.'
                      : 'Testimonial moderation is available on the Pro plan only.',
                  textAlign: TextAlign.center,
                ),
              ),
            );
          }
          return AppErrorWidget(
            message:
                isArabic ? 'خطأ في التحميل' : 'Error loading testimonials',
            onRetry: () => ref.invalidate(testimonialsProvider),
          );
        },
      ),
    );
  }

  Future<void> _moderate(BuildContext context, WidgetRef ref,
      PendingTestimonialModel t, bool isApproved, bool isArabic) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      final moderatorId = ref.read(authProvider).valueOrNull?.user?.id;
      if (moderatorId == null || moderatorId.isEmpty) {
        throw Exception(isArabic
            ? 'تعذر تحديد المستخدم الحالي'
            : 'Current user unavailable');
      }
      await apiClient.post<void>(
        ApiEndpoints.trainerTestimonials(TrainerId(t.trainerId)),
        data: {
          'testimonialId': t.id,
          'gymId': t.gymId,
          'isApproved': isApproved,
          'moderatedBy': moderatorId,
        },
        parser: (_) {},
      );
      ref.invalidate(testimonialsProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    }
  }
}
