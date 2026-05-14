import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../communication/presentation/providers/communication_providers.dart';
import '../providers/gamification_providers.dart';

class TrainerPublicProfileScreen extends ConsumerWidget {
  final String trainerId;

  const TrainerPublicProfileScreen({super.key, required this.trainerId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final profileAsync =
        ref.watch(trainerPublicProfileProvider(trainerId));

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ملف المدرب' : 'Trainer Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.chat_bubble_outline),
            tooltip: isArabic ? 'محادثة' : 'Chat',
            onPressed: () => _startChat(context, ref),
          ),
        ],
      ),
      body: profileAsync.when(
        data: (profile) => _ProfileContent(
            profile: profile, isArabic: isArabic),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(message: e.toString()),
      ),
    );
  }

  Future<void> _startChat(BuildContext context, WidgetRef ref) async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final auth = ref.read(authProvider).valueOrNull;
    final gymId = auth?.currentGymId;
    if (gymId == null) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'لا يوجد جيم نشط' : 'No active gym'),
      ));
      return;
    }
    try {
      final apiClient = ref.read(apiClientProvider);
      final conversationId = await apiClient.post<String>(
        ApiEndpoints.conversations,
        data: {'gymId': gymId, 'participant2': trainerId},
        parser: (data) => data.toString(),
      );
      ref.read(activeConversationIdProvider.notifier).state =
          conversationId;
      ref.invalidate(conversationsProvider);
      if (!context.mounted) return;
      context.go(AppRoutes.chat);
    } catch (_) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذر بدء المحادثة. حاول مرة أخرى.'
            : 'Could not start chat. Try again.'),
      ));
    }
  }
}

class _ProfileContent extends StatelessWidget {
  final TrainerPublicProfileModel profile;
  final bool isArabic;

  const _ProfileContent(
      {required this.profile, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: [
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                // Avatar
                AvatarWidget(
                  imageUrl: profile.trainerPhotoUrl,
                  name: profile.trainerName,
                  size: 96,
                ),
                const SizedBox(height: 12),
                Text(
                  profile.trainerName,
                  style: Theme.of(context)
                      .textTheme
                      .headlineSmall
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
                if (profile.specialization != null)
                  Text(
                    profile.specialization!,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color:
                              Theme.of(context).colorScheme.primary,
                        ),
                  ),

                const SizedBox(height: 16),

                // Stats row
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    _StatItem(
                      value:
                          profile.averageRating.toStringAsFixed(1),
                      label: isArabic ? 'التقييم' : 'Rating',
                      icon: Icons.star,
                      color: Colors.amber,
                    ),
                    _StatItem(
                      value: '${profile.totalReviews}',
                      label: isArabic ? 'تقييمات' : 'Reviews',
                      icon: Icons.rate_review_outlined,
                      color: Colors.blue,
                    ),
                    if (profile.totalClients != null)
                      _StatItem(
                        value: '${profile.totalClients}',
                        label: isArabic ? 'عملاء' : 'Clients',
                        icon: Icons.people_outline,
                        color: Colors.green,
                      ),
                    if (profile.totalSessions != null)
                      _StatItem(
                        value: '${profile.totalSessions}',
                        label: isArabic ? 'جلسات' : 'Sessions',
                        icon: Icons.fitness_center,
                        color: Colors.purple,
                      ),
                  ],
                ),

                if (profile.bio != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    profile.bio!,
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                ],
              ],
            ),
          ),
        ),

        // Badges
        if (profile.badges?.isNotEmpty == true) ...[
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
              child: Text(
                isArabic ? 'الشارات' : 'Badges',
                style: Theme.of(context)
                    .textTheme
                    .titleSmall
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: SizedBox(
              height: 80,
              child: ListView.builder(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: profile.badges!.length,
                itemBuilder: (context, index) {
                  final badge = profile.badges![index];
                  return Padding(
                    padding: const EdgeInsets.only(right: 12),
                    child: Column(
                      children: [
                        Container(
                          width: 48,
                          height: 48,
                          decoration: BoxDecoration(
                            color: Colors.amber.withOpacity(0.1),
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.emoji_events,
                              color: Colors.amber),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          badge.name,
                          style:
                              Theme.of(context).textTheme.bodySmall,
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
          ),
        ],

        // Testimonials
        if (profile.testimonials?.isNotEmpty == true) ...[
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Text(
                isArabic ? 'آراء العملاء' : 'Client Reviews',
                style: Theme.of(context)
                    .textTheme
                    .titleSmall
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
            ),
          ),
          SliverList(
            delegate: SliverChildBuilderDelegate(
              (context, index) {
                final t = profile.testimonials![index];
                return AppCard(
                  margin: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 4),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: List.generate(
                            t.stars,
                            (_) => const Icon(Icons.star,
                                color: Colors.amber, size: 16),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(t.content),
                      ],
                    ),
                  ),
                );
              },
              childCount: profile.testimonials!.length,
            ),
          ),
        ],

        const SliverToBoxAdapter(child: SizedBox(height: 24)),
      ],
    );
  }
}

class _StatItem extends StatelessWidget {
  final String value;
  final String label;
  final IconData icon;
  final Color color;

  const _StatItem({
    required this.value,
    required this.label,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, color: color, size: 20),
        const SizedBox(height: 4),
        Text(value,
            style: const TextStyle(
                fontWeight: FontWeight.w700, fontSize: 16)),
        Text(label,
            style: Theme.of(context).textTheme.bodySmall),
      ],
    );
  }
}
