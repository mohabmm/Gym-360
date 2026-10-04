import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/announcements_provider.dart';

/// Localizes the BE AnnouncementCategory enum (General/Schedule/Offer/
/// Maintenance) for the Arabic-first list. Previously the list rendered the
/// raw enum string ("General"), leaking English into the Arabic UI.
String _categoryLabel(String? category, bool isArabic) {
  switch ((category ?? 'general').toLowerCase()) {
    case 'schedule':
      return isArabic ? 'الجدول' : 'Schedule';
    case 'offer':
      return isArabic ? 'عرض' : 'Offer';
    case 'maintenance':
      return isArabic ? 'صيانة' : 'Maintenance';
    case 'general':
    default:
      return isArabic ? 'عام' : 'General';
  }
}

class AnnouncementListScreen extends ConsumerWidget {
  const AnnouncementListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final announcements = ref.watch(announcementsProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الاعلانات' : 'Announcements'),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.push(RoutePaths.createAnnouncement),
        child: const Icon(Icons.add),
      ),
      body: announcements.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.campaign_outlined,
              title: isArabic ? 'لا يوجد اعلانات' : 'No announcements',
              subtitle: isArabic
                  ? 'اضف اعلانا جديدا للبدء'
                  : 'Create a new announcement to get started',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(announcementsProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final item = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: theme.colorScheme.primary.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              _categoryLabel(item.category, isArabic),
                              style: theme.textTheme.labelSmall?.copyWith(
                                color: theme.colorScheme.primary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          const Spacer(),
                          Text(
                            item.createdAt != null
                                ? DateFormatter.formatShort(item.createdAt!)
                                : '',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        item.title,
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        item.content,
                        style: theme.textTheme.bodySmall,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
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
          message: isArabic ? 'خطأ في تحميل الاعلانات' : 'Error loading announcements',
          onRetry: () => ref.invalidate(announcementsProvider),
        ),
      ),
    );
  }
}
