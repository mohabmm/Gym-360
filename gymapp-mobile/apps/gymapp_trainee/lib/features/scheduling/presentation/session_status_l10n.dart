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
String sessionStatusLabel(String status, bool isArabic) =>
    _localized(_statusLabels, status, isArabic);

/// Normalized status → (Arabic, English).
const Map<String, (String, String)> _statusLabels = {
  'pending': ('قيد الانتظار', 'Pending'),
  'confirmed': ('مؤكد', 'Confirmed'),
  'inprogress': ('جارية', 'In Progress'),
  'completed': ('مكتمل', 'Completed'),
  'cancelled': ('ألغاه المدرب', 'Cancelled by trainer'),
  'cancelledbytrainer': ('ألغاه المدرب', 'Cancelled by trainer'),
  'cancelledbymember': ('ألغاه المتدرب', 'Cancelled by trainee'),
  'latecancelled': ('إلغاء متأخر', 'Late Cancelled'),
  'noshow': ('لم يحضر', 'No Show'),
};

/// Looks up [raw] (normalized) in [labels]; unknown values are returned as-is.
String _localized(
  Map<String, (String, String)> labels,
  String raw,
  bool isArabic,
) {
  final label = labels[_normalize(raw)];
  if (label == null) return raw;
  return isArabic ? label.$1 : label.$2;
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
String sessionTypeLabel(String type, bool isArabic) =>
    _localized(_typeLabels, type, isArabic);

/// Normalized session type → (Arabic, English).
const Map<String, (String, String)> _typeLabels = {
  'personaltraining': ('تدريب شخصي', 'Personal Training'),
  'assessment': ('تقييم', 'Assessment'),
  'followup': ('متابعة', 'Follow-up'),
};

/// Lowercases and strips separators so `Late_Cancelled`, `LATE-CANCELLED`,
/// `LateCancelled` and `late cancelled` all collapse to `latecancelled`.
String _normalize(String s) =>
    s.toLowerCase().replaceAll(RegExp(r'[\s_\-]'), '');
