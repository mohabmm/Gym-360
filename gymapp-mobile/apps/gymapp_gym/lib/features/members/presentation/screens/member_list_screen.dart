import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/members_provider.dart';

class MemberListScreen extends ConsumerStatefulWidget {
  const MemberListScreen({super.key});

  @override
  ConsumerState<MemberListScreen> createState() => _MemberListScreenState();
}

class _MemberListScreenState extends ConsumerState<MemberListScreen> {
  final _searchController = TextEditingController();
  final _debouncer = Debouncer(milliseconds: 400);

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final members = ref.watch(membersProvider);
    final filter = ref.watch(memberFilterProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final authState = ref.watch(authProvider).valueOrNull;
    final canAdd = authState?.hasPermission(StaffPermission.addMember) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الاعضاء' : 'Members'),
      ),
      floatingActionButton: canAdd
          ? FloatingActionButton(
              onPressed: () => context.push(RoutePaths.addMember),
              child: const Icon(Icons.person_add),
            )
          : null,
      body: Column(
        children: [
          // Search bar
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppTextField(
              controller: _searchController,
              hint: isArabic
                  ? 'بحث بالاسم او الهاتف او البريد'
                  : 'Search by name, phone, or email',
              prefixIcon: const Icon(Icons.search),
              onChanged: (value) {
                _debouncer.run(() {
                  ref.read(memberFilterProvider.notifier).state =
                      filter.copyWith(search: value, page: 1);
                });
              },
            ),
          ),

          // Filters
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                _FilterChip(
                  label: isArabic ? 'الكل' : 'All',
                  selected: filter.status == null,
                  onSelected: (_) {
                    ref.read(memberFilterProvider.notifier).state =
                        filter.withStatus(null);
                  },
                ),
                const SizedBox(width: 8),
                _FilterChip(
                  label: isArabic ? 'نشط' : 'Active',
                  selected: filter.status == AppConstants.memberActive,
                  onSelected: (_) {
                    ref.read(memberFilterProvider.notifier).state =
                        filter.copyWith(
                            status: AppConstants.memberActive, page: 1);
                  },
                ),
                const SizedBox(width: 8),
                _FilterChip(
                  label: isArabic ? 'مجمد' : 'Frozen',
                  selected: filter.status == AppConstants.memberFrozen,
                  onSelected: (_) {
                    ref.read(memberFilterProvider.notifier).state =
                        filter.copyWith(
                            status: AppConstants.memberFrozen, page: 1);
                  },
                ),
                const SizedBox(width: 8),
                _FilterChip(
                  label: isArabic ? 'منتهي' : 'Expired',
                  selected: filter.status == AppConstants.memberExpired,
                  onSelected: (_) {
                    ref.read(memberFilterProvider.notifier).state =
                        filter.copyWith(
                            status: AppConstants.memberExpired, page: 1);
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),

          // Member list
          Expanded(
            child: members.when(
              data: (result) {
                if (result.items.isEmpty) {
                  final isFiltered = filter.status != null ||
                      (filter.search?.trim().isNotEmpty ?? false);
                  if (isFiltered) {
                    return EmptyState(
                      icon: Icons.search_off,
                      title: isArabic ? 'لا توجد نتائج' : 'No results',
                      subtitle: isArabic
                          ? 'لا يوجد اعضاء مطابقون للبحث او التصفية الحالية'
                          : 'No members match the current search or filter',
                    );
                  }
                  return EmptyState(
                    icon: Icons.people_outline,
                    title: isArabic ? 'لا يوجد اعضاء' : 'No members found',
                    subtitle: isArabic
                        ? 'اضف اعضاء جدد للبدء'
                        : 'Add new members to get started',
                    actionLabel: canAdd
                        ? (isArabic ? 'اضافة عضو' : 'Add Member')
                        : null,
                    onAction:
                        canAdd ? () => context.push(RoutePaths.addMember) : null,
                  );
                }

                return RefreshIndicator(
                  onRefresh: () => ref.refresh(membersProvider.future),
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: result.items.length,
                    itemBuilder: (context, index) {
                      final member = result.items[index];
                      return _MemberCard(
                        member: member,
                        isArabic: isArabic,
                        onTap: () =>
                            context.push('/members/${member.id}'),
                      );
                    },
                  ),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => AppErrorWidget(
                message: isArabic
                    ? 'خطأ في تحميل الاعضاء'
                    : 'Error loading members',
                onRetry: () => ref.invalidate(membersProvider),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool selected;
  final ValueChanged<bool>? onSelected;

  const _FilterChip({
    required this.label,
    required this.selected,
    this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return FilterChip(
      label: Text(label),
      selected: selected,
      onSelected: onSelected,
    );
  }
}

class _MemberCard extends StatelessWidget {
  final UserModel member;
  final bool isArabic;
  final VoidCallback onTap;

  const _MemberCard({
    required this.member,
    required this.isArabic,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final membership = member.gymMemberships?.isNotEmpty == true
        ? member.gymMemberships!.first
        : null;

    return AppCard(
      onTap: onTap,
      margin: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          AvatarWidget(
            imageUrl: member.photoUrl,
            name: '${member.firstName} ${member.lastName}',
            size: AppConstants.avatarSizeMedium,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${member.firstName} ${member.lastName}',
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                if (member.phone != null)
                  Text(member.phone!, style: theme.textTheme.bodySmall),
                const SizedBox(height: 4),
                Row(
                  children: [
                    if (membership != null && membership.membershipType != null)
                      _PlanBadge(plan: membership.membershipType!),
                    const SizedBox(width: 8),
                    _StatusBadge(
                      status: member.status ?? AppConstants.memberActive,
                      isArabic: isArabic,
                    ),
                  ],
                ),
              ],
            ),
          ),
          if (member.memberNumber != null)
            Text(
              '#${member.memberNumber}',
              style: theme.textTheme.labelMedium?.copyWith(
                color: AppColors.grey500,
              ),
            ),
        ],
      ),
    );
  }
}

class _PlanBadge extends StatelessWidget {
  final String plan;

  const _PlanBadge({required this.plan});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.primary.withOpacity(0.1),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        plan,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w600,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;
  final bool isArabic;

  const _StatusBadge({required this.status, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    Color color;
    String label;
    switch (status) {
      case AppConstants.memberActive:
        color = AppColors.success;
        label = isArabic ? 'نشط' : 'Active';
        break;
      case AppConstants.memberFrozen:
        color = AppColors.info;
        label = isArabic ? 'مجمد' : 'Frozen';
        break;
      case AppConstants.memberExpired:
        color = AppColors.error;
        label = isArabic ? 'منتهي' : 'Expired';
        break;
      default:
        color = AppColors.grey400;
        label = isArabic ? 'معلق' : 'Pending';
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w600,
          color: color,
        ),
      ),
    );
  }
}
