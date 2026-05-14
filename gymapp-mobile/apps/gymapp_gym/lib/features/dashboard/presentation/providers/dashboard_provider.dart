import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../billing/presentation/providers/billing_provider.dart';

// ---------------------------------------------------------------------------
// Dashboard Data Model
// ---------------------------------------------------------------------------
class DashboardData {
  final int activeMembers;
  final double revenueThisMonth;
  final int todayCheckIns;
  final String? memberTrend;
  final String? revenueTrend;
  final List<RevenueDataPoint> revenueChart;
  final List<PeakHourEntry> peakHours;
  final List<TopClassEntry> topClasses;
  final List<AtRiskMember> atRiskMembers;
  final CoachOfMonthData? coachOfMonth;
  final List<ActivityEntry> recentActivity;

  const DashboardData({
    this.activeMembers = 0,
    this.revenueThisMonth = 0,
    this.todayCheckIns = 0,
    this.memberTrend,
    this.revenueTrend,
    this.revenueChart = const [],
    this.peakHours = const [],
    this.topClasses = const [],
    this.atRiskMembers = const [],
    this.coachOfMonth,
    this.recentActivity = const [],
  });
}

class RevenueDataPoint {
  final String label;
  final double amount;

  const RevenueDataPoint({required this.label, required this.amount});

  factory RevenueDataPoint.fromJson(Map<String, dynamic> json) {
    return RevenueDataPoint(
      label: json['label'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
    );
  }
}

class PeakHourEntry {
  final int hour;
  final int dayOfWeek;
  final int count;

  const PeakHourEntry({
    required this.hour,
    required this.dayOfWeek,
    required this.count,
  });

  factory PeakHourEntry.fromJson(Map<String, dynamic> json) {
    return PeakHourEntry(
      hour: json['hour'] as int? ?? 0,
      dayOfWeek: json['dayOfWeek'] as int? ?? 0,
      count: json['count'] as int? ?? 0,
    );
  }
}

class TopClassEntry {
  final String className;
  final String instructorName;
  final int bookings;

  const TopClassEntry({
    required this.className,
    required this.instructorName,
    required this.bookings,
  });

  factory TopClassEntry.fromJson(Map<String, dynamic> json) {
    return TopClassEntry(
      className: json['className'] as String? ?? '',
      instructorName: json['instructorName'] as String? ?? '',
      bookings: json['bookings'] as int? ?? 0,
    );
  }
}

class AtRiskMember {
  final String id;
  final String name;
  final String? photoUrl;
  final int daysSinceLastVisit;
  final String? planName;

  const AtRiskMember({
    required this.id,
    required this.name,
    this.photoUrl,
    required this.daysSinceLastVisit,
    this.planName,
  });

  factory AtRiskMember.fromJson(Map<String, dynamic> json) {
    return AtRiskMember(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      photoUrl: json['photoUrl'] as String?,
      daysSinceLastVisit: json['daysSinceLastVisit'] as int? ?? 0,
      planName: json['planName'] as String?,
    );
  }
}

class CoachOfMonthData {
  final String trainerId;
  final String trainerName;
  final String? trainerPhotoUrl;
  final double averageRating;
  final int sessionsCompleted;

  const CoachOfMonthData({
    required this.trainerId,
    required this.trainerName,
    this.trainerPhotoUrl,
    required this.averageRating,
    required this.sessionsCompleted,
  });

  factory CoachOfMonthData.fromJson(Map<String, dynamic> json) {
    return CoachOfMonthData(
      trainerId: json['trainerId'] as String? ?? '',
      trainerName: json['trainerName'] as String? ?? '',
      trainerPhotoUrl: json['trainerPhotoUrl'] as String?,
      averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0,
      sessionsCompleted: json['sessionsCompleted'] as int? ?? 0,
    );
  }
}

class ActivityEntry {
  final String type;
  final String description;
  final DateTime timestamp;

  const ActivityEntry({
    required this.type,
    required this.description,
    required this.timestamp,
  });

  factory ActivityEntry.fromJson(Map<String, dynamic> json) {
    return ActivityEntry(
      type: json['type'] as String? ?? '',
      description: json['description'] as String? ?? '',
      timestamp: DateTime.tryParse(json['timestamp'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}

// ---------------------------------------------------------------------------
// Dashboard Provider
// ---------------------------------------------------------------------------
final dashboardProvider = FutureProvider.autoDispose<DashboardData>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) {
    // No active gym yet — return empty data instead of hitting /gyms//analytics
    // (which collapses to /gyms/analytics and 404s).
    return const DashboardData();
  }

  final analyticsFuture = apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.gymAnalyticsByGym(gymId),
    parser: (data) => data as Map<String, dynamic>,
  );

  // Peak hours is Pro-gated on the BE. If the plan lacks it, swallow the
  // 403 so the rest of the dashboard still renders — the FeatureGate wrapper
  // around _PeakHoursHeatmap will show the upgrade CTA.
  final peakHoursFuture = apiClient.get<List<PeakHourEntry>>(
    ApiEndpoints.gymPeakHoursByGym(gymId),
    queryParameters: {
      'tzOffsetMinutes': DateTime.now().timeZoneOffset.inMinutes,
    },
    parser: (data) {
      final buckets = (data is Map<String, dynamic>)
          ? data['buckets'] as List<dynamic>?
          : null;
      return buckets
              ?.map((e) => PeakHourEntry.fromJson(e as Map<String, dynamic>))
              .toList() ??
          <PeakHourEntry>[];
    },
  ).catchError((Object e) {
    if (e is FeatureGatedException ||
        e is NotFoundException ||
        (e is ApiException && e.errorCode == 'PLAN.FEATURE_NOT_AVAILABLE')) {
      return <PeakHourEntry>[];
    }
    throw e;
  });

  final paymentsFuture = apiClient.get<List<PaymentModel>>(
    ApiEndpoints.payments,
    queryParameters: {'page': 1, 'pageSize': 500},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <PaymentModel>[];
      }
      return list
          .map((e) => PaymentModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  ).catchError((Object _) => <PaymentModel>[]);

  final results = await Future.wait([analyticsFuture, peakHoursFuture, paymentsFuture]);
  final analyticsData = results[0] as Map<String, dynamic>;
  final peakHours = results[1] as List<PeakHourEntry>;
  final payments = results[2] as List<PaymentModel>;

  final now = DateTime.now();
  final monthStart = DateTime(now.year, now.month, 1);
  final revenueThisMonth = payments
      .where((p) =>
          p.status.toLowerCase() == 'completed' &&
          p.createdAt != null &&
          p.createdAt!.isAfter(monthStart))
      .fold<double>(0, (sum, p) => sum + p.amount);

  return DashboardData(
    // BE /gyms/{id}/analytics emits totalMembers/checkInsToday — accept both
    // legacy and current field names so a backend-name change doesn't blank
    // the dashboard.
    activeMembers: (analyticsData['activeMembers'] as int?) ??
        (analyticsData['totalMembers'] as int?) ??
        0,
    revenueThisMonth: (analyticsData['revenueThisMonth'] as num?)?.toDouble() ??
        revenueThisMonth,
    todayCheckIns: (analyticsData['todayCheckIns'] as int?) ??
        (analyticsData['checkInsToday'] as int?) ??
        0,
    memberTrend: analyticsData['memberTrend'] as String?,
    revenueTrend: analyticsData['revenueTrend'] as String?,
    revenueChart: (analyticsData['revenueChart'] as List<dynamic>?)
            ?.map((e) =>
                RevenueDataPoint.fromJson(e as Map<String, dynamic>))
            .toList() ??
        [],
    peakHours: peakHours,
    topClasses: (analyticsData['topClasses'] as List<dynamic>?)
            ?.map(
                (e) => TopClassEntry.fromJson(e as Map<String, dynamic>))
            .toList() ??
        [],
    atRiskMembers: (analyticsData['atRiskMembers'] as List<dynamic>?)
            ?.map(
                (e) => AtRiskMember.fromJson(e as Map<String, dynamic>))
            .toList() ??
        [],
    coachOfMonth: analyticsData['coachOfMonth'] != null
        ? CoachOfMonthData.fromJson(
            analyticsData['coachOfMonth'] as Map<String, dynamic>,
          )
        : null,
    recentActivity: (analyticsData['recentActivity'] as List<dynamic>?)
            ?.map(
                (e) => ActivityEntry.fromJson(e as Map<String, dynamic>))
            .toList() ??
        [],
  );
});
