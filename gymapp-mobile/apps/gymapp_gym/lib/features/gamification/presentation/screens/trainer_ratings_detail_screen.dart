import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/gamification_provider.dart';

class TrainerRatingsDetailScreen extends ConsumerWidget {
  final String trainerId;

  const TrainerRatingsDetailScreen({super.key, required this.trainerId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ratings = ref.watch(trainerRatingsDetailProvider(trainerId));
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقييمات المدرب' : 'Trainer Ratings'),
      ),
      body: ratings.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.star_border,
              title: isArabic ? 'لا توجد تقييمات' : 'No ratings yet',
            );
          }

          final avg = items.map((r) => r.stars).reduce((a, b) => a + b) /
              items.length;

          return RefreshIndicator(
            onRefresh: () =>
                ref.refresh(trainerRatingsDetailProvider(trainerId).future),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Summary
                AppCard(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      Column(
                        children: [
                          Row(
                            children: [
                              const Icon(Icons.star,
                                  color: AppColors.star, size: 28),
                              const SizedBox(width: 4),
                              Text(
                                avg.toStringAsFixed(1),
                                style: theme.textTheme.headlineMedium?.copyWith(
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ],
                          ),
                          Text(
                            isArabic ? 'متوسط التقييم' : 'Average Rating',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                      Column(
                        children: [
                          Text(
                            '${items.length}',
                            style: theme.textTheme.headlineMedium?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          Text(
                            isArabic ? 'تقييم' : 'Reviews',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Individual ratings
                ...items.map((r) => AppCard(
                      margin: const EdgeInsets.only(bottom: 10),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              ...List.generate(
                                5,
                                (i) => Icon(
                                  i < r.stars ? Icons.star : Icons.star_border,
                                  color: AppColors.star,
                                  size: 16,
                                ),
                              ),
                              const Spacer(),
                              if (r.createdAt != null)
                                Text(
                                  DateFormatter.formatShort(r.createdAt!),
                                  style: theme.textTheme.bodySmall,
                                ),
                            ],
                          ),
                          if (r.comment != null && r.comment!.isNotEmpty) ...[
                            const SizedBox(height: 8),
                            Text(r.comment!, style: theme.textTheme.bodyMedium),
                          ],
                        ],
                      ),
                    )),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading ratings',
          onRetry: () => ref.invalidate(trainerRatingsDetailProvider(trainerId)),
        ),
      ),
    );
  }
}
