import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/trainers_provider.dart';

class TrainerDetailScreen extends ConsumerWidget {
  final String trainerId;

  const TrainerDetailScreen({super.key, required this.trainerId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trainer = ref.watch(trainerDetailProvider(trainerId));
    // Ratings live behind pt.coach_ratings: with it off the ratings route redirects
    // to the dashboard, so the action (and the request) must not exist at all.
    final ratingsOn = ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptCoachRatings);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ملف المدرب' : 'Trainer Profile'),
        actions: [
          if (ratingsOn)
            IconButton(
              icon: const Icon(Icons.star_rate),
              onPressed: () => context.push('/gamification/ratings/$trainerId'),
              tooltip: isArabic ? 'التقييمات' : 'Ratings',
            ),
        ],
      ),
      body: trainer.when(
        data: (t) => RefreshIndicator(
          onRefresh: () async => ref.invalidate(trainerDetailProvider(trainerId)),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppCard(
                child: Column(
                  children: [
                    AvatarWidget(
                      imageUrl: t.photoUrl,
                      name: '${t.firstName} ${t.lastName}',
                      size: 72,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      '${t.firstName} ${t.lastName}',
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    if (t.phone != null) ...[
                      const SizedBox(height: 4),
                      Text(t.phone!, style: theme.textTheme.bodySmall),
                    ],
                    if (t.email.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(t.email, style: theme.textTheme.bodySmall),
                    ],
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (ratingsOn) _RatingsSummary(trainerId: trainerId, isArabic: isArabic),
            ],
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل الملف' : 'Error loading profile',
          onRetry: () => ref.invalidate(trainerDetailProvider(trainerId)),
        ),
      ),
    );
  }
}

/// Average stars and review count (pt.coach_ratings).
class _RatingsSummary extends ConsumerWidget {
  final String trainerId;
  final bool isArabic;

  const _RatingsSummary({required this.trainerId, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    return ref.watch(trainerRatingsProvider(trainerId)).when(
          data: (ratingList) {
            if (ratingList.isEmpty) return const SizedBox.shrink();
            final avg = ratingList.map((r) => r.stars.toDouble()).reduce((a, b) => a + b) / ratingList.length;
            return AppCard(
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: [
                  Column(
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.star, color: AppColors.star, size: 20),
                          const SizedBox(width: 4),
                          Text(
                            avg.toStringAsFixed(1),
                            style: theme.textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ],
                      ),
                      Text(
                        isArabic ? 'متوسط التقييم' : 'Avg Rating',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                  Column(
                    children: [
                      Text(
                        '${ratingList.length}',
                        style: theme.textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w700,
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
            );
          },
          loading: () => const SizedBox.shrink(),
          error: (_, __) => const SizedBox.shrink(),
        );
  }
}
