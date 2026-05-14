import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/gamification_providers.dart';

class MyRatingsDashboardScreen extends ConsumerWidget {
  const MyRatingsDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final summaryAsync = ref.watch(myRatingsSummaryProvider);
    final testimonialsAsync = ref.watch(myTestimonialsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقييماتي' : 'My Ratings'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(myRatingsSummaryProvider);
          ref.invalidate(myTestimonialsProvider);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Summary card
            summaryAsync.when(
              data: (data) {
                final avg =
                    (data['displayRating'] as num?)?.toDouble() ??
                    (data['averageRating'] as num?)?.toDouble() ?? 0.0;
                final count =
                    (data['totalRatings'] as num?)?.toInt() ?? 0;
                final coachCount =
                    (data['coachOfTheMonthCount'] as num?)?.toInt() ?? 0;

                // Hide the phantom "4.8★ with 0 تقييم" state: if no ratings
                // have been recorded yet, don't render the score block.
                // Treat both an explicit 0-count and a 0 average as "no
                // ratings" — the BE public profile doesn't always return a
                // ratings count, so `avg == 0` is our primary signal.
                if (count == 0 && avg == 0) {
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        children: [
                          Icon(
                            Icons.star_border_rounded,
                            size: 48,
                            color: AppColors.grey400,
                          ),
                          const SizedBox(height: 8),
                          Text(
                            isArabic ? 'لا توجد تقييمات بعد' : 'No ratings yet',
                            style: theme.textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            isArabic
                                ? 'ستظهر تقييمات العملاء هنا بعد الجلسات'
                                : "Client ratings will appear here after sessions",
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: AppColors.grey500,
                            ),
                            textAlign: TextAlign.center,
                          ),
                          if (coachCount > 0) ...[
                            const Divider(height: 24),
                            _StatItem(
                              value: '$coachCount',
                              label: isArabic
                                  ? 'مدرب الشهر'
                                  : 'Coach of the Month',
                              color: Colors.amber,
                            ),
                          ],
                        ],
                      ),
                    ),
                  );
                }

                return Card(
                  child: Padding(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      children: [
                        // Stars display
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: List.generate(5, (i) {
                            final filled = i < avg.floor();
                            final half = !filled &&
                                i < avg &&
                                (avg - i) >= 0.5;
                            return Icon(
                              filled
                                  ? Icons.star
                                  : half
                                      ? Icons.star_half
                                      : Icons.star_border,
                              color: Colors.amber,
                              size: 32,
                            );
                          }),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          avg.toStringAsFixed(1),
                          style:
                              theme.textTheme.displaySmall?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        Text(
                          '$count ${isArabic ? 'تقييم' : 'ratings'}',
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(color: AppColors.grey500),
                        ),
                        if (coachCount > 0) ...[
                          const Divider(height: 24),
                          Row(
                            mainAxisAlignment:
                                MainAxisAlignment.spaceAround,
                            children: [
                              _StatItem(
                                value: '$coachCount',
                                label: isArabic
                                    ? 'مرات مدرب الشهر'
                                    : 'Times Coach of the Month',
                                color: Colors.amber,
                              ),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                );
              },
              loading: () => const ShimmerCard(height: 200),
              error: (_, __) => const SizedBox.shrink(),
            ),
            const SizedBox(height: 16),

            // Testimonials list (anonymized, from public profile)
            Text(
              isArabic ? 'اراء العملاء' : 'Client Testimonials',
              style: theme.textTheme.titleMedium
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            testimonialsAsync.when(
              data: (testimonials) {
                if (testimonials.isEmpty) {
                  return EmptyState(
                    icon: Icons.star_outline,
                    title:
                        isArabic ? 'لا توجد اراء بعد' : 'No testimonials yet',
                    subtitle: isArabic
                        ? 'ستظهر اراء العملاء المعتمدة هنا'
                        : 'Approved client testimonials will appear here',
                  );
                }

                return Column(
                  children: testimonials
                      .map((t) => _TestimonialCard(
                            testimonial: t,
                            isArabic: isArabic,
                          ))
                      .toList(),
                );
              },
              loading: () => Column(
                children: List.generate(
                  3,
                  (_) => const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: ShimmerCard(height: 100),
                  ),
                ),
              ),
              error: (e, _) => AppErrorWidget(
                message: e.toString(),
                onRetry: () => ref.invalidate(myTestimonialsProvider),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatItem extends StatelessWidget {
  final String value;
  final String label;
  final Color color;

  const _StatItem({
    required this.value,
    required this.label,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Text(
          value,
          style: theme.textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.w700,
            color: color,
          ),
        ),
        Text(label, style: theme.textTheme.bodySmall),
      ],
    );
  }
}

class _TestimonialCard extends StatelessWidget {
  final Map<String, dynamic> testimonial;
  final bool isArabic;

  const _TestimonialCard({required this.testimonial, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final comment = testimonial['comment'] as String? ??
        testimonial['text'] as String? ??
        '';

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.format_quote, color: AppColors.grey400),
                const SizedBox(width: 8),
                Text(
                  isArabic ? 'رأي عميل' : 'Client Testimonial',
                  style: theme.textTheme.titleSmall
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
              ],
            ),
            if (comment.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(
                comment,
                style: theme.textTheme.bodyMedium?.copyWith(
                  fontStyle: FontStyle.italic,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
