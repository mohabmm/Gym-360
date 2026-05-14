import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/staff_provider.dart';

class StaffListScreen extends ConsumerWidget {
  const StaffListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final staff = ref.watch(staffListProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider).valueOrNull;
    final canAdd = authState?.hasPermission(StaffPermission.addStaff) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الموظفون' : 'Staff'),
        actions: [
          IconButton(
            icon: const Icon(Icons.security),
            tooltip: isArabic ? 'صلاحيات' : 'Permissions',
            onPressed: () => context.push(RoutePaths.permissionMatrix),
          ),
        ],
      ),
      floatingActionButton: canAdd
          ? FloatingActionButton(
              onPressed: () => context.push(RoutePaths.addStaff),
              child: const Icon(Icons.person_add),
            )
          : null,
      body: staff.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.admin_panel_settings_outlined,
              title: isArabic ? 'لا يوجد موظفون' : 'No staff members',
              subtitle: isArabic ? 'اضف موظفين للصالة' : 'Add staff to your gym',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(staffListProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final s = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: AvatarWidget(
                      imageUrl: s.photoUrl,
                      name: s.name,
                      size: 48,
                    ),
                    title: Text(
                      s.name,
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                    ),
                    subtitle: Text(
                      _roleLabel(s.staffRole, isArabic),
                    ),
                    trailing: s.isActive
                        ? Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: AppColors.success.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              isArabic ? 'نشط' : 'Active',
                              style: const TextStyle(
                                color: AppColors.success,
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          )
                        : null,
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading staff',
          onRetry: () => ref.invalidate(staffListProvider),
        ),
      ),
    );
  }

  String _roleLabel(String role, bool isArabic) {
    switch (role.toLowerCase()) {
      case AppConstants.roleOwner:
        return isArabic ? 'مالك' : 'Owner';
      case AppConstants.roleManager:
        return isArabic ? 'مدير' : 'Manager';
      case AppConstants.roleReceptionist:
        return isArabic ? 'موظف استقبال' : 'Receptionist';
      default:
        return role;
    }
  }
}
