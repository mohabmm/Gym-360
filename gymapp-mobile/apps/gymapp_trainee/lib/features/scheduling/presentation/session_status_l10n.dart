import 'package:flutter/material.dart';

/// Localized labels/colors for the BE SessionStatus enum (PascalCase):
/// Pending, Confirmed, InProgress, Completed, CancelledByTrainer,
/// CancelledByMember, LateCancelled, NoShow.
///
/// BUG: previously the trainee app only matched a handful of single-word
/// statuses (`scheduled`/`confirmed`/`completed`/`cancelled`/`pending`) plus
/// snake_case `late_cancelled`/`no_show`. The real BE enum is compound
/// PascalCase, so `CancelledByMember`, `CancelledByTrainer`, `LateCancelled`,
/// `NoShow` and `InProgress` all fell through to the default branch and were
/// rendered as raw English inside the Arabic-first UI. This single source of
/// truth normalizes the status and covers every enum value.
String sessionStatusLabel(String status, bool isArabic) {
  switch (_normalize(status)) {
    case 'pending':
      return isArabic ? 'قيد الانتظار' : 'Pending';
    case 'confirmed':
      return isArabic ? 'مؤكد' : 'Confirmed';
    case 'inprogress':
      return isArabic ? 'جارية' : 'In Progress';
    case 'completed':
      return isArabic ? 'مكتمل' : 'Completed';
    case 'cancelled':
    case 'cancelledbytrainer':
      return isArabic ? 'ألغاه المدرب' : 'Cancelled by trainer';
    case 'cancelledbymember':
      return isArabic ? 'ألغاه المتدرب' : 'Cancelled by trainee';
    case 'latecancelled':
      return isArabic ? 'إلغاء متأخر' : 'Late Cancelled';
    case 'noshow':
      return isArabic ? 'لم يحضر' : 'No Show';
    default:
      return status;
  }
}

Color sessionStatusColor(String status) {
  switch (_normalize(status)) {
    case 'completed':
      return Colors.green;
    case 'cancelled':
    case 'cancelledbytrainer':
    case 'cancelledbymember':
      return Colors.red;
    case 'latecancelled':
      return Colors.orange;
    case 'noshow':
      return Colors.grey;
    case 'inprogress':
      return Colors.teal;
    default:
      return Colors.blue;
  }
}

/// Localizes the BE SessionType enum (PersonalTraining/Assessment/FollowUp).
/// The session-detail screen previously printed the raw enum string, leaking
/// English into the Arabic-first UI. Labels mirror the booking sheet.
String sessionTypeLabel(String type, bool isArabic) {
  switch (_normalize(type)) {
    case 'personaltraining':
      return isArabic ? 'تدريب شخصي' : 'Personal Training';
    case 'assessment':
      return isArabic ? 'تقييم' : 'Assessment';
    case 'followup':
      return isArabic ? 'متابعة' : 'Follow-up';
    default:
      return type;
  }
}

/// Lowercases and strips separators so `Late_Cancelled`, `LATE-CANCELLED`,
/// `LateCancelled` and `late cancelled` all collapse to `latecancelled`.
String _normalize(String s) =>
    s.toLowerCase().replaceAll(RegExp(r'[\s_\-]'), '');
