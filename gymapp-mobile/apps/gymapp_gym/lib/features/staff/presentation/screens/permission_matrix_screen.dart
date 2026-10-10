import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';

class PermissionMatrixScreen extends ConsumerWidget {
  const PermissionMatrixScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    final roles = [
      AppConstants.roleOwner,
      AppConstants.roleManager,
      AppConstants.roleReceptionist,
    ];
    final permissions = StaffPermission.values;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'مصفوفة الصلاحيات' : 'Permission Matrix'),
      ),
      body: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: SingleChildScrollView(
          child: DataTable(
            headingRowColor: WidgetStateProperty.all(
              theme.colorScheme.primary.withOpacity(0.08),
            ),
            columns: [
              DataColumn(
                label: Text(
                  isArabic ? 'الصلاحية' : 'Permission',
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              ...roles.map((r) => DataColumn(
                    label: Text(
                      StaffPermissions.roleDisplayName(r, isArabic: isArabic),
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  )),
            ],
            rows: permissions.map((p) {
              return DataRow(
                cells: [
                  DataCell(
                    Text(
                      _permissionLabel(p, isArabic),
                      style: theme.textTheme.bodySmall,
                    ),
                  ),
                  ...roles.map((r) {
                    final has = StaffPermissions.hasPermission(r, p);
                    return DataCell(
                      Icon(
                        has ? Icons.check_circle : Icons.cancel_outlined,
                        color: has ? AppColors.success : AppColors.grey400,
                        size: 20,
                      ),
                    );
                  }),
                ],
              );
            }).toList(),
          ),
        ),
      ),
    );
  }

  String _permissionLabel(StaffPermission p, bool isArabic) {
    if (isArabic) {
      switch (p) {
        case StaffPermission.viewDashboard: return 'عرض الرئيسية';
        case StaffPermission.viewAnalytics: return 'عرض التحليلات';
        case StaffPermission.viewMembers: return 'عرض الاعضاء';
        case StaffPermission.addMember: return 'اضافة عضو';
        case StaffPermission.editMember: return 'تعديل عضو';
        case StaffPermission.freezeMember: return 'تجميد عضوية';
        case StaffPermission.deleteMember: return 'حذف عضو';
        case StaffPermission.viewTrainers: return 'عرض المدربين';
        case StaffPermission.approveTrainer: return 'الموافقة على مدرب';
        case StaffPermission.viewClasses: return 'عرض الحصص';
        case StaffPermission.createClass: return 'انشاء حصة';
        case StaffPermission.editClass: return 'تعديل حصة';
        case StaffPermission.cancelClass: return 'الغاء حصة';
        case StaffPermission.viewExercises: return 'عرض التمارين';
        case StaffPermission.createCustomExercise: return 'انشاء تمرين مخصص';
        case StaffPermission.viewCheckInLog: return 'عرض سجل الحضور';
        case StaffPermission.manualCheckIn: return 'تسجيل حضور يدوي';
        case StaffPermission.kioskMode: return 'وضع الكيوسك';
        case StaffPermission.recordPayment: return 'تسجيل دفع';
        case StaffPermission.viewPayments: return 'عرض المدفوعات';
        case StaffPermission.managePackageTemplates: return 'ادارة باقات الحصص';
        case StaffPermission.assignPackage: return 'تعيين باقة';
        case StaffPermission.viewRevenue: return 'عرض الايرادات';
        case StaffPermission.viewAnnouncements: return 'عرض الاعلانات';
        case StaffPermission.createAnnouncement: return 'انشاء اعلان';
        case StaffPermission.viewLeaderboards: return 'عرض لوحة المتصدرين';
        case StaffPermission.viewTrainerRatings: return 'عرض تقييمات المدربين';
        case StaffPermission.moderateTestimonials: return 'مراجعة آراء العملاء';
        case StaffPermission.viewStaff: return 'عرض الموظفين';
        case StaffPermission.addStaff: return 'اضافة موظف';
        case StaffPermission.editStaff: return 'تعديل موظف';
        case StaffPermission.viewSettings: return 'عرض الاعدادات';
        case StaffPermission.editGymProfile: return 'تعديل ملف الصالة';
        case StaffPermission.editBranding: return 'تعديل العلامة التجارية';
        case StaffPermission.editSessionPolicies: return 'تعديل سياسات الحصص';
        case StaffPermission.editSections: return 'تعديل الاقسام';
        case StaffPermission.editOperatingHours: return 'تعديل ساعات العمل';
        case StaffPermission.editSeasonalHours: return 'تعديل ساعات موسمية';
        case StaffPermission.editAmenities: return 'تعديل المرافق';
        case StaffPermission.editNotificationSettings: return 'تعديل الاشعارات';
      }
    }
    return p.name.replaceAllMapped(
      RegExp(r'([A-Z])'),
      (m) => ' ${m.group(0)}',
    );
  }
}
