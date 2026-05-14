import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/trainers_provider.dart';

class TrainerListScreen extends ConsumerWidget {
  const TrainerListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trainers = ref.watch(trainersProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المدربون' : 'Trainers'),
        actions: [
          TextButton(
            onPressed: () => context.push(RoutePaths.pendingTrainers),
            child: Text(isArabic ? 'طلبات الانضمام' : 'Pending'),
          ),
        ],
      ),
      body: trainers.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.fitness_center_outlined,
              title: isArabic ? 'لا يوجد مدربون' : 'No trainers',
              subtitle: isArabic ? 'لا يوجد مدربون مسجلون في صالتك' : 'No trainers registered at your gym',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(trainersProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final t = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  onTap: () => context.push('/trainers/${t.id}'),
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
                      const Icon(Icons.chevron_right),
                    ],
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل المدربين' : 'Error loading trainers',
          onRetry: () => ref.invalidate(trainersProvider),
        ),
      ),
    );
  }
}
